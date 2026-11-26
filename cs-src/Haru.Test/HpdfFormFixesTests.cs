using System;
using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Forms;
using Haru.Forms.Appearance;
using Haru.Objects;
using Haru.Types;
using Xunit;

namespace Haru.Test
{
    /// <summary>
    /// Tests for repeatable annotation setters, widget validation, and the field rules of radio buttons,
    /// choice fields, checkboxes, signatures and the form.
    /// </summary>
    public class HpdfFormFixesTests
    {
        private static readonly HpdfRect Box = new HpdfRect(100, 100, 120, 120);

        // ---------- annotation setters ----------

        [Fact]
        public void SetBorderStyle_Twice_ReplacesStyle()
        {
            var widget = NewWidget();
            widget.SetBorderStyle(HpdfBorderStyle.Solid);

            widget.SetBorderStyle(HpdfBorderStyle.Dashed, 2);

            var bs = (HpdfDict)widget.Dict["BS"];
            ((HpdfName)bs["S"]).Value.Should().Be("D");
            ((HpdfReal)bs["W"]).Value.Should().Be(2);
        }

        [Fact]
        public void SetBorderStyle_Invalid_LeavesAnnotationUnchanged()
        {
            var widget = NewWidget();

            Action badStyle = () => widget.SetBorderStyle((HpdfBorderStyle)42);
            Action badWidth = () => widget.SetBorderStyle(HpdfBorderStyle.Solid, -1);

            badStyle.Should().Throw<HpdfException>();
            badWidth.Should().Throw<HpdfException>();
            widget.Dict.ContainsKey("BS").Should().BeFalse();
        }

        [Fact]
        public void SetColor_RgbThenCmyk_ReplacesColor()
        {
            var widget = NewWidget();
            widget.SetRgbColor(1, 0, 0);

            widget.SetCmykColor(0, 0, 0, 1);

            ((HpdfArray)widget.Dict["C"]).Count.Should().Be(4);
        }

        [Fact]
        public void LinkAndTextAnnotationSetters_CanBeCalledAgain()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var link = page.CreateLinkAnnotation(Box, "https://example.com");
            var note = page.CreateTextAnnotation(Box, "Note");

            link.SetHighlightMode(HpdfHighlightMode.InvertBox);
            link.SetHighlightMode(HpdfHighlightMode.NoHighlight);
            note.SetOpened(true);
            note.SetOpened(false);

            ((HpdfName)link.Dict["H"]).Value.Should().Be("N");
            ((HpdfBoolean)note.Dict["Open"]).Value.Should().BeFalse();
        }

        // ---------- widget appearance ----------

        [Theory]
        [InlineData(new[] { 1f, 0f })]
        [InlineData(new[] { 1f, 0f, 0f, 0f })]
        [InlineData(new[] { 1.5f, 0f, 0f })]
        [InlineData(new[] { -0.1f, 0f, 0f })]
        public void SetAppearanceCharacteristics_InvalidColor_Throws(float[] color)
        {
            var widget = NewWidget();

            Action act = () => widget.SetAppearanceCharacteristics(borderColor: color);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void RotatedCheckbox_RotatesItsStateAppearances()
        {
            var checkbox = new HpdfCheckbox(new HpdfDocument().Xref, "Agree");
            var widget = checkbox.CreateWidget(new HpdfRect(0, 0, 40, 20));

            widget.SetAppearanceCharacteristics(borderColor: new[] { 0f, 0f, 0f }, rotation: 90);

            var on = (HpdfStreamObject)((HpdfDict)((HpdfDict)widget.Dict["AP"])["N"])["Yes"];
            ((HpdfArray)on["BBox"]).Cast<HpdfReal>().Select(v => v.Value).Should().Equal(0f, 0f, 20f, 40f);
            ((HpdfArray)on["Matrix"]).Cast<HpdfNumber>().Select(v => v.Value).Should().Equal(0, 1, -1, 0, 0, 0);
        }

        [Fact]
        public void SetButtonAppearances_SameOnAndOffName_Throws()
        {
            var widget = NewWidget();

            Action act = () => widget.SetButtonAppearances(ButtonElementType.Checkbox, "Off", "Off");

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void SetButtonAppearances_CustomGenerator_IsUsed()
        {
            var widget = NewWidget();

            widget.SetButtonAppearances(HpdfAppearanceFactory.CreateRadioButtonAppearance(0.8f), "On", "Off");

            ((HpdfDict)((HpdfDict)widget.Dict["AP"])["N"]).Keys.Should().BeEquivalentTo("On", "Off");
            ((HpdfString)((HpdfDict)widget.Dict["MK"])["CA"]).GetText().Should().Be("l");
        }

        // ---------- adding widgets ----------

        [Fact]
        public void AddWidget_SameWidgetTwice_AddsItOnce()
        {
            var field = new HpdfTextField(new HpdfDocument().Xref, "Name");
            var widget = field.CreateWidget(Box);

            field.AddWidget(widget);

            field.Widgets.Should().HaveCount(1);
            ((HpdfArray)field.Dict["Kids"]).Count.Should().Be(1);
        }

        [Fact]
        public void AddWidget_WidgetOfAnotherField_Throws()
        {
            var doc = new HpdfDocument();
            var first = new HpdfTextField(doc.Xref, "First");
            var second = new HpdfTextField(doc.Xref, "Second");
            var widget = first.CreateWidget(Box);

            Action act = () => second.AddWidget(widget);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
            second.Widgets.Should().BeEmpty();
        }

        [Fact]
        public void AddWidgetAnnotation_SamePageTwice_AddsOnce_OtherPageThrows()
        {
            var doc = new HpdfDocument();
            var page1 = doc.AddPage();
            var page2 = doc.AddPage();
            var widget = new HpdfTextField(doc.Xref, "Name").CreateWidget(Box);
            page1.AddWidgetAnnotation(widget);

            page1.AddWidgetAnnotation(widget);
            Action otherPage = () => page2.AddWidgetAnnotation(widget);

            ((HpdfArray)page1.Dict["Annots"]).Count.Should().Be(1);
            otherPage.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        // ---------- radio buttons ----------

        [Theory]
        [InlineData("Off")]
        [InlineData("")]
        [InlineData(null)]
        public void CreateOption_InvalidName_ThrowsBeforeCreatingObjects(string? name)
        {
            var doc = new HpdfDocument();
            var radio = new HpdfRadioButton(doc.Xref, "Color");
            int objects = doc.Xref.Entries.Count;

            Action act = () => radio.CreateOption(name!, Box);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            doc.Xref.Entries.Count.Should().Be(objects);
        }

        [Fact]
        public void CreateOption_DuplicateName_Throws()
        {
            var radio = new HpdfRadioButton(new HpdfDocument().Xref, "Color");
            radio.CreateOption("Red", Box);

            Action act = () => radio.CreateOption("Red", Box);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            radio.Widgets.Should().HaveCount(1);
        }

        [Fact]
        public void RadioButton_PlainWidgets_AreRejected()
        {
            var doc = new HpdfDocument();
            var radio = new HpdfRadioButton(doc.Xref, "Color");
            var stray = new HpdfWidgetAnnotation(doc.Xref, Box);

            Action create = () => radio.CreateWidget(Box);
            Action add = () => radio.AddWidget(stray);

            create.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
            add.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
            radio.Widgets.Should().BeEmpty();
        }

        [Fact]
        public void RadioButton_FlagsAssignment_KeepsRadioFlag()
        {
            var radio = new HpdfRadioButton(new HpdfDocument().Xref, "Color");

            radio.Flags = HpdfFieldFlags.Required;

            radio.Flags.Should().HaveFlag(HpdfFieldFlags.Radio).And.HaveFlag(HpdfFieldFlags.Required);
        }

        [Fact]
        public void RadioButton_SetDefaultValue_WritesStateName()
        {
            var radio = new HpdfRadioButton(new HpdfDocument().Xref, "Color");
            radio.CreateOption("Red", Box);

            radio.SetDefaultValue("Red");

            ((HpdfName)radio.Dict["DV"]).Value.Should().Be("Red");
        }

        // ---------- choice fields ----------

        [Fact]
        public void SetSelectedValues_SeveralOnSingleSelect_ThrowsAndKeepsSelection()
        {
            var list = new HpdfChoice(new HpdfDocument().Xref, "Colors", isComboBox: false);
            list.AddOptions("Red", "Green");
            list.SetValue("Red");

            Action act = () => list.SetSelectedValues("Red", "Green");

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
            ((HpdfString)list.Dict["V"]).GetText().Should().Be("Red");
        }

        [Fact]
        public void ClearOptions_ClearsSelectionOfRemovedOptions()
        {
            var combo = new HpdfChoice(new HpdfDocument().Xref, "Country");
            combo.AddOptions("Canada", "USA");
            combo.SetValue("USA");

            combo.ClearOptions();

            combo.Dict.ContainsKey("V").Should().BeFalse();
        }

        [Fact]
        public void EditableComboBox_AcceptsValueOutsideOptions()
        {
            var combo = new HpdfChoice(new HpdfDocument().Xref, "Country");
            combo.AddOptions("Canada", "USA");
            combo.SetEditable(true);

            combo.SetValue("Mexico");
            combo.ClearOptions();

            ((HpdfString)combo.Dict["V"]).GetText().Should().Be("Mexico", "a typed value does not depend on the options");
        }

        [Fact]
        public void ComboBox_FlagsAssignment_KeepsComboFlag()
        {
            var combo = new HpdfChoice(new HpdfDocument().Xref, "Country");

            combo.Flags = HpdfFieldFlags.Required;

            combo.Flags.Should().HaveFlag(HpdfFieldFlags.Combo);
        }

        // ---------- checkboxes, signatures, form ----------

        [Fact]
        public void Checkbox_SetDefaultValue_WritesStateNameOrThrows()
        {
            var checkbox = new HpdfCheckbox(new HpdfDocument().Xref, "Agree");

            checkbox.SetDefaultValue("Yes");
            Action invalid = () => checkbox.SetDefaultValue("On");

            ((HpdfName)checkbox.Dict["DV"]).Value.Should().Be("Yes");
            invalid.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void Signature_CannotGetTextValue_ButCanBeRequired()
        {
            var signature = new HpdfSignature(new HpdfDocument().Xref, "Signature");

            Action value = () => signature.SetValue("signed");
            Action defaultValue = () => signature.SetDefaultValue("signed");
            signature.SetRequired(true);

            value.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
            defaultValue.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
            signature.Dict.ContainsKey("V").Should().BeFalse();
            signature.Flags.Should().HaveFlag(HpdfFieldFlags.Required);
        }

        [Fact]
        public void AcroForm_WithoutFields_WritesEmptyFieldsArray()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            var form = doc.GetOrCreateAcroForm();

            doc.SaveToMemory();

            ((HpdfArray)form.Dict["Fields"]).Count.Should().Be(0);
        }

        [Fact]
        public void AcroForm_SetDefaultAppearanceNull_GivesEmptyString()
        {
            var form = new HpdfDocument().GetOrCreateAcroForm();

            form.SetDefaultAppearance(null!);

            form.DefaultAppearance.Should().BeEmpty();
            form.Dict.ContainsKey("DA").Should().BeFalse();
        }

        private static HpdfWidgetAnnotation NewWidget() => new HpdfWidgetAnnotation(new HpdfDocument().Xref, Box);
    }
}
