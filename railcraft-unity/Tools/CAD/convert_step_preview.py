"""Convert a STEP assembly into a hierarchy-preserving GLB for Unity preview.

OpenCascade performs the CAD read and tessellation.  Blender performs the
final FBX optimization in ``Tools/Blender/export_bogie_preview.py``.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
import time
from pathlib import Path


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("input", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--ocp-path", type=Path, required=True)
    parser.add_argument("--linear-deflection", type=float, default=2.0)
    parser.add_argument("--angular-deflection", type=float, default=0.4)
    return parser.parse_args()


def main() -> int:
    args = parse_arguments()
    sys.path.insert(0, str(args.ocp_path.resolve()))

    from OCP.BRepMesh import BRepMesh_IncrementalMesh
    from OCP.Message import Message_ProgressRange
    from OCP.RWGltf import RWGltf_CafWriter
    from OCP.RWMesh import RWMesh_CoordinateSystem
    from OCP.STEPCAFControl import STEPCAFControl_Reader
    from OCP.TCollection import TCollection_AsciiString, TCollection_ExtendedString
    from OCP.TColStd import TColStd_IndexedDataMapOfStringString
    from OCP.TDataStd import TDataStd_Name
    from OCP.TDF import TDF_Label, TDF_LabelSequence, TDF_Tool
    from OCP.TDocStd import TDocStd_Document
    from OCP.XCAFApp import XCAFApp_Application
    from OCP.XCAFDoc import XCAFDoc_DocumentTool, XCAFDoc_ShapeTool

    source = args.input.resolve()
    output = args.output.resolve()
    report_path = args.report.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    started = time.time()

    app = XCAFApp_Application.GetApplication_s()
    document = TDocStd_Document(TCollection_ExtendedString("RailCraftCadPreview"))
    app.NewDocument(TCollection_ExtendedString("MDTV-XCAF"), document)

    reader = STEPCAFControl_Reader()
    reader.SetNameMode(True)
    reader.SetColorMode(True)
    reader.SetLayerMode(True)
    reader.SetMatMode(True)
    read_status = reader.ReadFile(str(source))
    if not reader.Transfer(document):
        raise RuntimeError(f"STEP XCAF transfer failed: {read_status}")

    shape_tool = XCAFDoc_DocumentTool.ShapeTool_s(document.Main())
    roots = TDF_LabelSequence()
    shape_tool.GetFreeShapes(roots)
    if roots.Length() == 0:
        raise RuntimeError("STEP XCAF document contains no free shapes")

    def label_name(label: TDF_Label) -> str:
        attribute = TDataStd_Name()
        if label.FindAttribute(TDataStd_Name.GetID_s(), attribute):
            return attribute.Get().ToExtString()
        return ""

    def effective_name(label: TDF_Label) -> str:
        direct = label_name(label)
        if direct:
            return direct
        referred = TDF_Label()
        if XCAFDoc_ShapeTool.GetReferredShape_s(label, referred):
            return label_name(referred)
        return ""

    names: list[dict[str, object]] = []
    visited_entries: set[str] = set()

    def walk(label: TDF_Label, depth: int) -> None:
        entry_value = TCollection_AsciiString()
        TDF_Tool.Entry_s(label, entry_value)
        entry = entry_value.ToCString()
        if entry in visited_entries:
            return
        visited_entries.add(entry)
        name = effective_name(label)
        if name:
            names.append({"depth": depth, "name": name, "entry": entry})
        components = TDF_LabelSequence()
        if XCAFDoc_ShapeTool.GetComponents_s(label, components, False):
            for index in range(1, components.Length() + 1):
                component = components.Value(index)
                referred = TDF_Label()
                if XCAFDoc_ShapeTool.GetReferredShape_s(component, referred):
                    walk(referred, depth + 1)
                else:
                    walk(component, depth + 1)

    for index in range(1, roots.Length() + 1):
        walk(roots.Value(index), 0)

    shape = shape_tool.GetOneShape()
    mesher = BRepMesh_IncrementalMesh(
        shape,
        args.linear_deflection,
        False,
        args.angular_deflection,
        True,
    )
    mesher.Perform()
    if not mesher.IsDone():
        raise RuntimeError("OpenCascade meshing did not complete")

    writer = RWGltf_CafWriter(TCollection_AsciiString(str(output)), True)
    converter = writer.ChangeCoordinateSystemConverter()
    converter.SetInputCoordinateSystem(
        RWMesh_CoordinateSystem.RWMesh_CoordinateSystem_Zup
    )
    converter.SetOutputCoordinateSystem(
        RWMesh_CoordinateSystem.RWMesh_CoordinateSystem_glTF
    )
    converter.SetInputLengthUnit(0.001)
    converter.SetOutputLengthUnit(1.0)
    file_info = TColStd_IndexedDataMapOfStringString()
    if not writer.Perform(document, file_info, Message_ProgressRange()):
        raise RuntimeError("glTF export failed")

    report = {
        "schema": 1,
        "source_file": source.name,
        "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
        "output": output.name,
        "output_bytes": output.stat().st_size,
        "read_status": str(read_status),
        "root_count": roots.Length(),
        "label_count": len(names),
        "labels": names,
        "linear_deflection_mm": args.linear_deflection,
        "angular_deflection_radians": args.angular_deflection,
        "elapsed_seconds": round(time.time() - started, 3),
    }
    report_path.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print("STEP_PREVIEW_CONVERSION_SUCCEEDED")
    print(json.dumps(report, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
