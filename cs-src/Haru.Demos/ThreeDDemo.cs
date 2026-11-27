/*
 * Demonstrates 3D annotations: a PRC model with views, a comment note measurement with its projection
 * annotation, and a text note that switches to a view. Viewers that show 3D: Adobe Acrobat and Reader
 * (3D content must be enabled in the preferences), Foxit; other viewers show the poster.
 */

using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.ThreeD;
using Haru.Types;

namespace BasicDemos
{
    public static class ThreeDDemo
    {
        public static void Run()
        {
            try
            {
                var pdf = new HpdfDocument();
                pdf.SetCompressionMode(HpdfCompressionMode.All);
                var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
                var boldFont = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");

                var page = pdf.AddPage();
                float top = page.Height;
                DrawLabel(page, boldFont, 50, top - 50, "3D Annotations", 22);
                DrawLabel(page, font, 50, top - 72,
                    "A PRC model (OpTeX pdfextra example) with views; click it to rotate it in Acrobat or Reader", 11, 0.4f);

                // The model and its views: the first view keeps the camera of the model file
                var model = pdf.Load3DModel("demo/3d/part.prc");
                var modelView = model.CreateView("Camera of the model");
                modelView.SetLighting(Hpdf3DLighting.Artwork);

                var cadView = model.CreateView("CAD lighting, blue background");
                cadView.SetLighting(Hpdf3DLighting.CAD);
                cadView.SetBackgroundColor(0.85f, 0.9f, 1);

                var dayView = model.CreateView("Day lighting, orthographic");
                dayView.SetLighting(Hpdf3DLighting.Day);
                dayView.SetOrthographicProjection(1);
                dayView.SetBackgroundColor(1, 1, 0.92f);

                var area = new HpdfRect(50, 330, 545, 750);
                var annotation = page.Create3DAnnotation(area, model, "A machine part (PRC)");
                annotation.SetActivation(Hpdf3DActivation.PageOpen);
                annotation.SetUserInterface(showToolbar: true, showNavigationPanel: true);

                // A comment note in the model, listed among the comments through its projection annotation
                var commentNote = model.CreateCommentNote(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(20, 20, 20));
                commentNote.SetName("Origin");
                commentNote.SetText("The origin of the model");
                commentNote.SetColor(0.8f, 0, 0);
                modelView.AddMeasure(commentNote);
                page.CreateProjectionAnnotation(area, "The origin of the model", commentNote);

                // A sticky note that shows a view when it is selected in the comments list
                var note = page.CreateTextAnnotation(new HpdfRect(50, 295, 70, 315),
                    "Select this comment to see the model in CAD lighting", HpdfAnnotationIcon.Comment);
                note.SetTitle("Reviewer");
                note.Set3DView(annotation, cadView);

                DrawLabel(page, font, 80, 300, "A comment that switches to the view \"CAD lighting, blue background\"", 10);
                DrawLabel(page, font, 50, 260, "The views are listed in the 3D toolbar of the viewer; the model tree shows the parts.", 10);
                DrawLabel(page, font, 50, 245, "Viewers without 3D show the poster: a gray box with a cube.", 10);

                pdf.SaveToFile("pdfs/ThreeDDemo.pdf");
                Console.WriteLine("ThreeDDemo completed successfully!");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error in ThreeDDemo: {e.Message}");
                Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
                throw;
            }
        }

        private static void DrawLabel(HpdfPage page, HpdfFont font, float x, float y, string text, float size, float gray = 0)
        {
            page.SetFontAndSize(font, size);
            page.SetGrayFill(gray);
            page.BeginText();
            page.MoveTextPos(x, y);
            page.ShowText(text);
            page.EndText();
        }
    }
}
