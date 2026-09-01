"""Convert the inspected CW-200K STEP assembly to a hierarchy-preserving GLB.

Requires Python 3.12 and the precompiled cadquery-ocp package. The GLB is an
intermediate file consumed by Tools/Blender/export_cw200k_reference.py.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path


SOURCE_STEP_SHA256 = "36f1413e2d04be22d234ba74832a54047a12f6f505202b7e9d55b883be74a3f1"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("input", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--ocp-path", type=Path, required=True)
    parser.add_argument("--linear-deflection", type=float, default=2.0)
    parser.add_argument("--angular-deflection", type=float, default=0.45)
    args = parser.parse_args()

    source = args.input.resolve()
    source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
    if source_hash != SOURCE_STEP_SHA256:
        raise RuntimeError(
            f"CW-200K source hash changed: {source_hash}; expected {SOURCE_STEP_SHA256}"
        )

    sys.path.insert(0, str(args.ocp_path.resolve()))
    from OCP.BRepMesh import BRepMesh_IncrementalMesh
    from OCP.Message import Message_ProgressRange
    from OCP.RWGltf import RWGltf_CafWriter
    from OCP.RWMesh import RWMesh_CoordinateSystem
    from OCP.STEPCAFControl import STEPCAFControl_Reader
    from OCP.TCollection import TCollection_AsciiString, TCollection_ExtendedString
    from OCP.TColStd import TColStd_IndexedDataMapOfStringString
    from OCP.TDataStd import TDataStd_Name
    from OCP.TDF import TDF_Label, TDF_LabelSequence
    from OCP.TDocStd import TDocStd_Document
    from OCP.XCAFApp import XCAFApp_Application
    from OCP.XCAFDoc import XCAFDoc_DocumentTool, XCAFDoc_ShapeTool

    output = args.output.resolve()
    report_path = args.report.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)

    application = XCAFApp_Application.GetApplication_s()
    document = TDocStd_Document(TCollection_ExtendedString("RailCraftCad"))
    application.NewDocument(TCollection_ExtendedString("MDTV-XCAF"), document)

    reader = STEPCAFControl_Reader()
    reader.SetNameMode(True)
    reader.SetColorMode(True)
    reader.SetLayerMode(True)
    reader.SetMatMode(True)
    status = reader.ReadFile(str(source))
    if not reader.Transfer(document):
        raise RuntimeError(f"STEP XCAF transfer failed: {status}")

    shape_tool = XCAFDoc_DocumentTool.ShapeTool_s(document.Main())
    roots = TDF_LabelSequence()
    shape_tool.GetFreeShapes(roots)
    if roots.Length() != 1:
        raise RuntimeError(f"Expected one CW-200K root assembly, found {roots.Length()}")

    def label_name(label: TDF_Label) -> str:
        attribute = TDataStd_Name()
        if label.FindAttribute(TDataStd_Name.GetID_s(), attribute):
            return attribute.Get().ToExtString()
        referred = TDF_Label()
        if XCAFDoc_ShapeTool.GetReferredShape_s(label, referred):
            if referred.FindAttribute(TDataStd_Name.GetID_s(), attribute):
                return attribute.Get().ToExtString()
        return ""

    component_names = []
    components = TDF_LabelSequence()
    XCAFDoc_ShapeTool.GetComponents_s(roots.Value(1), components, True)
    for index in range(1, components.Length() + 1):
        name = label_name(components.Value(index))
        if name:
            component_names.append(name)

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
    if not writer.Perform(
        document,
        TColStd_IndexedDataMapOfStringString(),
        Message_ProgressRange(),
    ):
        raise RuntimeError("glTF export failed")

    report = {
        "schema": 1,
        "source": source.name,
        "source_sha256": SOURCE_STEP_SHA256,
        "root_name": label_name(roots.Value(1)),
        "component_names": sorted(set(component_names)),
        "linear_deflection_mm": args.linear_deflection,
        "angular_deflection_radians": args.angular_deflection,
        "output": output.name,
        "output_bytes": output.stat().st_size,
        "output_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
    }
    report_path.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print("CW200K_STEP_CONVERSION_SUCCEEDED")
    print(json.dumps(report, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
