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
        /// <summary>
        /// The camera of the perspective views looks at the origin from this distance, far enough to show the whole
        /// model of the OpTeX pdfextra example, which is centered there.
        /// </summary>
        private const float OrbitRadius = 1000;

        /// <summary>
        /// The orbit of the orthographic view, as in the pdfextra example (the distance does not scale it).
        /// </summary>
        private const float OrthographicOrbitRadius = 400;

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
                    "A PRC model (OpTeX pdfextra example) with views; drag it to rotate it in Acrobat or Reader", 11, 0.4f);

                // The model and its views. A PRC file has no camera of its own: every view sets one, or the
                // viewer may show nothing
                var model = pdf.Load3DModel("demo/3d/part.prc");
                var origin = new HpdfPoint3D(0, 0, 0);

                var isometric = model.CreateView("Isometric");
                isometric.SetCamera(origin, new HpdfPoint3D(1, -1, 1), OrbitRadius, 0);
                isometric.SetPerspectiveProjection(30);
                isometric.SetLighting(Hpdf3DLighting.CAD);
                isometric.SetBackgroundColor(1, 1, 1);

                var front = model.CreateView("Front, blue background");
                front.SetCamera(origin, new HpdfPoint3D(0, -1, 0), OrbitRadius, 0);
                front.SetPerspectiveProjection(30);
                front.SetLighting(Hpdf3DLighting.Day);
                front.SetBackgroundColor(0.85f, 0.9f, 1);

                var topView = model.CreateView("Top, orthographic");
                topView.SetCamera(origin, new HpdfPoint3D(0, 0, 1), OrthographicOrbitRadius, 0);
                topView.SetOrthographicProjection(1);
                topView.SetLighting(Hpdf3DLighting.Headlamp);
                topView.SetBackgroundColor(1, 1, 0.92f);

                var area = new HpdfRect(50, 330, 545, 750);
                var annotation = page.Create3DAnnotation(area, model, "A machine part (PRC)");
                annotation.SetActivation(Hpdf3DActivation.PageOpen);
                annotation.SetUserInterface(showToolbar: true, showNavigationPanel: true);
                annotation.SetDefaultView(isometric);

                // A comment note in the model, listed among the comments through its projection annotation
                var commentNote = model.CreateCommentNote(origin, new HpdfPoint3D(60, -60, 60));
                commentNote.SetName("Origin");
                commentNote.SetText("The origin of the model");
                commentNote.SetColor(0.8f, 0, 0);
                isometric.AddMeasure(commentNote);
                page.CreateProjectionAnnotation("The origin of the model", commentNote);

                // A sticky note that shows a view when it is selected in the comments list
                var note = page.CreateTextAnnotation(new HpdfRect(50, 295, 70, 315),
                    "Select this comment to see the model from the front", HpdfAnnotationIcon.Comment);
                note.SetTitle("Reviewer");
                note.Set3DView(annotation, front);

                DrawLabel(page, font, 80, 300, "A comment that switches to the view \"Front, blue background\"", 10);
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
