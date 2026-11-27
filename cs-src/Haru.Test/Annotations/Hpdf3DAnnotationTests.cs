using System;
using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Objects;
using Haru.ThreeD;
using Haru.Types;
using Xunit;
using static Haru.Test.Annotations.HpdfShapeAnnotationTests;

namespace Haru.Test.Annotations
{
    public class Hpdf3DAnnotationTests
    {
        // The library does not read the model: the signature of the data is enough
        private static readonly byte[] U3dData = { (byte)'U', (byte)'3', (byte)'D', 0, 1, 2, 3, 4 };
        private static readonly byte[] PrcData = { (byte)'P', (byte)'R', (byte)'C', (byte)'4', 5, 6 };
        private static readonly HpdfRect Area = new HpdfRect(50, 400, 550, 750);

        [Fact]
        public void Load3DModel_U3d_WritesStreamAndRequiresPdf16()
        {
            var doc = new HpdfDocument();

            var model = doc.Load3DModel(U3dData);

            model.Format.Should().Be(Hpdf3DFormat.U3D);
            ((HpdfName)model.Stream["Type"]).Value.Should().Be("3D");
            ((HpdfName)model.Stream["Subtype"]).Value.Should().Be("U3D");
            model.Stream.Filter.Should().Be(Haru.Streams.HpdfStreamFilter.FlateDecode);
            model.Stream.Stream.ToArray().Should().Equal(U3dData);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version16);
            doc.Xref.RequiredAdobeExtensionLevel.Should().Be(0);
        }

        [Fact]
        public void Load3DModel_Prc_RequiresExtensionLevel3()
        {
            var doc = new HpdfDocument();
            doc.AddPage();

            var model = doc.Load3DModel(PrcData);
            doc.SaveToMemory();

            model.Format.Should().Be(Hpdf3DFormat.PRC);
            ((HpdfName)model.Stream["Subtype"]).Value.Should().Be("PRC");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version17);
            var adobe = (HpdfDict)((HpdfDict)doc.Catalog.Dict["Extensions"])["ADBE"];
            ((HpdfName)adobe["BaseVersion"]).Value.Should().Be("1.7");
            ((HpdfNumber)adobe["ExtensionLevel"]).Value.Should().Be(3);
        }

        [Fact]
        public void Load3DModel_OtherData_Throws()
        {
            var doc = new HpdfDocument();

            doc.Invoking(d => d.Load3DModel(new byte[] { 1, 2, 3, 4 })).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidU3dData);
            doc.Invoking(d => d.Load3DModel((byte[])null!)).Should().Throw<HpdfException>();
            doc.Invoking(d => d.Load3DModel("missing-" + Guid.NewGuid().ToString("N") + ".u3d")).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.FileNotFound);
        }

        [Fact]
        public void CreateView_AddsViewsAndDefault()
        {
            var model = new HpdfDocument().Load3DModel(U3dData);

            var front = model.CreateView("Front");
            var top = model.CreateView("Top");
            model.SetDefaultView(top);

            model.Views.Should().Equal(front, top);
            ((HpdfArray)model.Stream["VA"]).Should().HaveCount(2);
            ((HpdfNumber)model.Stream["DV"]).Value.Should().Be(1);
            ((HpdfName)front.Dict["Type"]).Value.Should().Be("3DView");
            ((HpdfString)front.Dict["XN"]).GetText().Should().Be("Front");
            ((HpdfString)front.Dict["IN"]).GetText().Should().Be("Front");
            model.Invoking(m => m.SetDefaultView(new HpdfDocument().Load3DModel(U3dData).CreateView("x"))).Should().Throw<HpdfException>();
            model.Invoking(m => m.CreateView("")).Should().Throw<HpdfException>();
        }

        [Fact]
        public void SetCamera_WritesAnOrthonormalCameraMatrix()
        {
            var view = new HpdfDocument().Load3DModel(U3dData).CreateView("v");

            view.SetCamera(new HpdfPoint3D(1, 2, 3), new HpdfPoint3D(1, -1, 0.5f), 100, 30);

            ((HpdfName)view.Dict["MS"]).Value.Should().Be("M");
            ((HpdfReal)view.Dict["CO"]).Value.Should().Be(100);
            float[] m = Numbers(view.Dict["C2W"]);
            m.Should().HaveCount(12);
            double Dot(int a, int b) => m[a] * m[b] + m[a + 1] * m[b + 1] + m[a + 2] * m[b + 2];
            Dot(0, 0).Should().BeApproximately(1, 1e-5);
            Dot(3, 3).Should().BeApproximately(1, 1e-5);
            Dot(6, 6).Should().BeApproximately(1, 1e-5);
            Dot(0, 3).Should().BeApproximately(0, 1e-5, "the roll keeps left and up perpendicular");
            Dot(0, 6).Should().BeApproximately(0, 1e-5);
            Dot(3, 6).Should().BeApproximately(0, 1e-5);
            // The camera is at the center of the orbit less the radius along the view
            double viewLength = Math.Sqrt(1 + 1 + 0.25);
            m[9].Should().BeApproximately((float)(1 + 100 * 1 / viewLength), 1e-3f);
        }

        [Fact]
        public void SetCamera_TopView()
        {
            var view = new HpdfDocument().Load3DModel(U3dData).CreateView("v");

            view.SetCamera(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(0, 0, 1), 10, 0);

            Numbers(view.Dict["C2W"]).Should().Equal(-1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, -1f, 0f, 0f, 10f);
        }

        [Fact]
        public void Projections_BackgroundAndLighting()
        {
            var doc = new HpdfDocument();
            var view = doc.Load3DModel(U3dData).CreateView("v");

            view.SetPerspectiveProjection(45);
            var projection = (HpdfDict)view.Dict["P"];
            ((HpdfName)projection["Subtype"]).Value.Should().Be("P");
            ((HpdfName)projection["PS"]).Value.Should().Be("Min");
            ((HpdfReal)projection["FOV"]).Value.Should().Be(45);

            view.SetOrthographicProjection(2);
            projection = (HpdfDict)view.Dict["P"];
            ((HpdfName)projection["Subtype"]).Value.Should().Be("O");
            ((HpdfReal)projection["OS"]).Value.Should().Be(2);

            view.SetBackgroundColor(1, 0.5f, 0);
            Numbers(((HpdfDict)view.Dict["BG"])["C"]).Should().Equal(1f, 0.5f, 0f);

            view.SetLighting(Hpdf3DLighting.Headlamp);
            ((HpdfName)((HpdfDict)view.Dict["LS"])["Subtype"]).Value.Should().Be("Headlamp");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version17);

            view.Invoking(v => v.SetPerspectiveProjection(200)).Should().Throw<HpdfException>();
            view.Invoking(v => v.SetOrthographicProjection(0)).Should().Throw<HpdfException>();
            view.Invoking(v => v.SetBackgroundColor(2, 0, 0)).Should().Throw<HpdfException>();
            view.Invoking(v => v.SetLighting((Hpdf3DLighting)20)).Should().Throw<HpdfException>();
        }

        [Fact]
        public void CrossSectionAndNodes()
        {
            var view = new HpdfDocument().Load3DModel(U3dData).CreateView("v");

            view.SetCrossSection(new HpdfPoint3D(0, 0, 5), 10, 20, 0.5f, true);
            var section = (HpdfDict)((HpdfArray)view.Dict["SA"])[0];
            ((HpdfName)section["Type"]).Value.Should().Be("3DCrossSection");
            Numbers(section["C"]).Should().Equal(0f, 0f, 5f);
            var orientation = (HpdfArray)section["O"];
            orientation[0].Should().BeOfType<HpdfNull>();
            ((HpdfReal)section["PO"]).Value.Should().Be(0.5f);
            ((HpdfBoolean)section["IV"]).Value.Should().BeTrue();
            view.RemoveCrossSection();
            ((HpdfArray)view.Dict["SA"]).Should().BeEmpty();

            var node = view.CreateNode("Lid");
            node.SetOpacity(0.3f);
            node.SetVisibility(false);
            node.SetMatrix(Hpdf3DMatrix.Identity);
            var nodeDict = (HpdfDict)((HpdfArray)view.Dict["NA"])[0];
            ((HpdfName)nodeDict["Type"]).Value.Should().Be("3DNode");
            ((HpdfString)nodeDict["N"]).GetText().Should().Be("Lid");
            ((HpdfReal)nodeDict["O"]).Value.Should().Be(0.3f);
            ((HpdfBoolean)nodeDict["V"]).Value.Should().BeFalse();
            Numbers(nodeDict["M"]).Should().HaveCount(12);
            view.Nodes.Should().Equal(node);
        }

        [Fact]
        public void SetOnInstantiate_WritesScriptStream()
        {
            var model = new HpdfDocument().Load3DModel(U3dData);

            model.SetOnInstantiate("scene.renderMode = \"wireframe\";");
            model.SetOnInstantiate("runtime.play();");

            var script = (HpdfStreamObject)model.Stream["OnInstantiate"];
            System.Text.Encoding.ASCII.GetString(script.Stream.ToArray()).Should().Be("runtime.play();");
        }

        [Fact]
        public void Create3DAnnotation_WritesActivationAndAppearance()
        {
            var doc = new HpdfDocument();
            var model = doc.Load3DModel(U3dData);
            var annot = doc.AddPage().Create3DAnnotation(Area, model, "Part");

            doc.SaveToMemory();

            annot.Type.Should().Be(HpdfAnnotationType.ThreeD);
            ((HpdfName)annot.Dict["Subtype"]).Value.Should().Be("3D");
            annot.Dict["3DD"].Should().BeSameAs(model.Stream);
            var activation = (HpdfDict)annot.Dict["3DA"];
            ((HpdfName)activation["A"]).Value.Should().Be("PO");
            ((HpdfName)activation["AIS"]).Value.Should().Be("L");
            ((HpdfName)activation["D"]).Value.Should().Be("PC");
            ((HpdfName)activation["DIS"]).Value.Should().Be("U");
            annot.Flags.Should().Be(HpdfAnnotationFlags.Print);
            AppearanceContent(annot).Should().Contain("0.92 g");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version16);
        }

        [Fact]
        public void ActivationUserInterfaceAndDefaultView()
        {
            var doc = new HpdfDocument();
            var model = doc.Load3DModel(U3dData);
            var view = model.CreateView("v");
            var annot = doc.AddPage().Create3DAnnotation(Area, model);

            annot.SetActivation(Hpdf3DActivation.Explicit, Hpdf3DArtworkState.Instantiated);
            annot.SetDeactivation(Hpdf3DDeactivation.PageInvisible, Hpdf3DArtworkState.Live);
            annot.SetUserInterface(false, true);
            annot.SetDefaultView(view);

            var activation = (HpdfDict)annot.Dict["3DA"];
            ((HpdfName)activation["A"]).Value.Should().Be("XA");
            ((HpdfName)activation["AIS"]).Value.Should().Be("I");
            ((HpdfName)activation["D"]).Value.Should().Be("PI");
            ((HpdfName)activation["DIS"]).Value.Should().Be("L");
            ((HpdfBoolean)activation["TB"]).Value.Should().BeFalse();
            ((HpdfBoolean)activation["NP"]).Value.Should().BeTrue();
            annot.Dict["3DV"].Should().BeSameAs(view.Dict);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version17);
            annot.Invoking(a => a.SetActivation(Hpdf3DActivation.PageOpen, Hpdf3DArtworkState.Uninstantiated)).Should().Throw<HpdfException>();
            annot.Invoking(a => a.SetDefaultView(doc.Load3DModel(U3dData).CreateView("other"))).Should().Throw<HpdfException>();
        }

        [Fact]
        public void PosterImage_IsDrawnScaledToTheRect()
        {
            var doc = new HpdfDocument();
            using var image = HpdfImage.LoadRawImageFromMem(doc.Xref, "Poster", new byte[] { 255, 0, 0, 0, 255, 0 }, 2, 1,
                HpdfColorSpace.DeviceRgb, 8);
            var annot = doc.AddPage().Create3DAnnotation(Area, doc.Load3DModel(U3dData), "Part", image);

            doc.SaveToMemory();

            var xobjects = (HpdfDict)((HpdfDict)Appearance(annot)["Resources"])["XObject"];
            xobjects["Poster"].Should().BeSameAs(image.Dict);
            AppearanceContent(annot).Should().Contain("500 0 0 350 50 400 cm\n/Poster Do");
        }

        [Fact]
        public void Markup_Set3DView_WritesExData()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var model = doc.Load3DModel(U3dData);
            var view = model.CreateView("Top");
            var annot3d = page.Create3DAnnotation(Area, model);
            var note = page.CreateTextAnnotation(new HpdfRect(10, 10, 30, 30), "See the top");

            note.Set3DView(annot3d, view);

            var exData = (HpdfDict)note.Dict["ExData"];
            ((HpdfName)exData["Subtype"]).Value.Should().Be("Markup3D");
            exData["3DA"].Should().BeSameAs(annot3d.Dict);
            exData["3DV"].Should().BeSameAs(view.Dict);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version17);
            note.Invoking(n => n.Set3DView(annot3d, doc.Load3DModel(U3dData).CreateView("x"))).Should().Throw<HpdfException>();
        }

        [Fact]
        public void Measures_AndProjectionAnnotation()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var model = doc.Load3DModel(U3dData);
            var view = model.CreateView("v");

            var note = model.CreateCommentNote(new HpdfPoint3D(1, 2, 3), new HpdfPoint3D(4, 5, 6));
            note.SetText("Check this edge");
            note.SetName("Note 1");
            note.SetColor(1, 0, 0);
            note.SetTextSize(10);
            note.SetTextBoxSize(100, 40);
            var dimension = model.CreatePerpendicularMeasure(new HpdfPoint3D(0, 0, 1), new HpdfPoint3D(0, 0, 0),
                new HpdfPoint3D(10, 0, 0), new HpdfPoint3D(0, 1, 0), new HpdfPoint3D(5, 2, 0), new HpdfPoint3D(0, 1, 0), 10, "mm");
            view.AddMeasure(note);
            view.AddMeasure(dimension);
            view.AddMeasure(note);
            var projection = page.CreateProjectionAnnotation("Check this edge", note);
            doc.SaveToMemory();

            ((HpdfName)note.Dict["Type"]).Value.Should().Be("3DMeasure");
            ((HpdfName)note.Dict["Subtype"]).Value.Should().Be("3DC");
            Numbers(note.Dict["A1"]).Should().Equal(1f, 2f, 3f);
            Numbers(note.Dict["TP"]).Should().Equal(4f, 5f, 6f);
            ((HpdfString)note.Dict["UT"]).GetText().Should().Be("Check this edge");
            ((HpdfString)note.Dict["TRL"]).GetText().Should().Be("Note 1");
            Numbers(note.Dict["C"]).Should().Equal(1f, 0f, 0f);
            Numbers(note.Dict["TB"]).Should().Equal(100f, 40f);
            ((HpdfName)dimension.Dict["Subtype"]).Value.Should().Be("PD3");
            ((HpdfReal)dimension.Dict["V"]).Value.Should().Be(10);
            ((HpdfString)dimension.Dict["U"]).GetText().Should().Be("mm");
            ((HpdfArray)view.Dict["MA"]).Should().HaveCount(2);

            projection.Type.Should().Be(HpdfAnnotationType.Projection);
            ((HpdfName)projection.Dict["Subtype"]).Value.Should().Be("Projection");
            var exData = (HpdfDict)projection.Dict["ExData"];
            ((HpdfName)exData["Subtype"]).Value.Should().Be("3DM");
            exData["M3DREF"].Should().BeSameAs(note.Dict);
            note.Dict["S"].Should().BeSameAs(projection.Dict);
            note.ProjectionAnnotation.Should().BeSameAs(projection);
            projection.Dict.ContainsKey("AP").Should().BeFalse("a projection annotation has no appearance");
            Numbers(projection.Dict["Rect"]).Should().Equal(0f, 0f, 0f, 0f);
            ((HpdfNumber)((HpdfDict)((HpdfDict)doc.Catalog.Dict["Extensions"])["ADBE"])["ExtensionLevel"]).Value.Should().Be(3);

            page.Invoking(p => p.CreateProjectionAnnotation("again", note)).Should().Throw<HpdfException>();
            projection.Invoking(p => p.Set3DView(page.Create3DAnnotation(Area, model), view)).Should().Throw<HpdfException>();
        }

        [Fact]
        public void ObjectsOfAnotherModelOrDocument_AreRejected()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var model = doc.Load3DModel(U3dData);
            var view = model.CreateView("v");
            var otherDoc = new HpdfDocument();
            var otherModel = otherDoc.Load3DModel(U3dData);
            var otherNote = otherModel.CreateCommentNote(default, new HpdfPoint3D(1, 1, 1));
            using var otherPoster = HpdfImage.LoadRawImageFromMem(otherDoc.Xref, "P", new byte[] { 0, 0, 0 }, 1, 1,
                HpdfColorSpace.DeviceRgb, 8);
            var otherAnnotation = otherDoc.AddPage().Create3DAnnotation(Area, otherModel);

            view.Invoking(v => v.AddMeasure(otherNote)).Should().Throw<HpdfException>();
            page.Invoking(p => p.Create3DAnnotation(Area, otherModel)).Should().Throw<HpdfException>();
            page.Invoking(p => p.Create3DAnnotation(Area, model, "x", otherPoster)).Should().Throw<HpdfException>();
            page.CreateTextAnnotation(new HpdfRect(1, 1, 2, 2), "n")
                .Invoking(n => n.Set3DView(otherAnnotation, otherModel.CreateView("o"))).Should().Throw<HpdfException>();
            otherNote.Model.Should().BeSameAs(otherModel);
        }

        [Fact]
        public void SetCameraMatrix_NotFinite_LeavesTheViewUnchanged()
        {
            var view = new HpdfDocument().Load3DModel(U3dData).CreateView("v");

            var act = () => view.SetCameraMatrix(new Hpdf3DMatrix(float.NaN, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0), 1);

            act.Should().Throw<HpdfException>();
            view.Dict.ContainsKey("MS").Should().BeFalse();
        }

        [Fact]
        public void PdfA_DoesNotAllow3D()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            doc.AddPage().Create3DAnnotation(Area, doc.Load3DModel(U3dData));

            var act = () => doc.SaveToMemory();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var model = doc.Load3DModel(PrcData);
            var view = model.CreateView("v");
            view.SetCamera(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(0, -1, 0), 50, 0);
            var page = doc.AddPage();
            page.Create3DAnnotation(Area, model).SetDefaultView(view);
            page.CreateProjectionAnnotation("n", model.CreateCommentNote(default, new HpdfPoint3D(1, 1, 1)));

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }
    }
}
