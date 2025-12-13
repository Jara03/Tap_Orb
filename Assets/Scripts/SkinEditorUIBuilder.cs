using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkinEditorUIBuilder
{
    private readonly Canvas canvas;
    private readonly Image backgroundImage;

    public SkinEditorUIBuilder(Canvas canvas)
    {
        this.canvas = canvas;
        backgroundImage = canvas.transform.Find("backgroundNeon")?.GetComponent<Image>();
    }

    public void Build()
    {
        var existing = canvas.transform.Find("SkinEditorPanel");
        if (existing != null) return;

        var openButton = CreateButton("OpenSkinEditorButton", canvas.transform, new Vector2(-350, 600), new Vector2(220, 80), "Skin Editor");
        GameObject panel = CreatePanel(canvas.transform);
        panel.SetActive(false);

        var controller = panel.AddComponent<SkinEditorUIController>();
        controller.Initialize(CreateUI(panel.transform), openButton, backgroundImage);
    }

    private GameObject CreatePanel(Transform parent)
    {
        var panel = new GameObject("SkinEditorPanel", typeof(RectTransform), typeof(Image));
        var rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(650, 900);
        rect.anchoredPosition = new Vector2(0, 0);

        var image = panel.GetComponent<Image>();
        image.color = new Color(0, 0, 0, 0.5f);

        return panel;
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string text)
    {
        var buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = buttonObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;

        var img = buttonObj.GetComponent<Image>();
        img.color = new Color(0.95f, 0.95f, 0.95f, 0.9f);

        var btn = buttonObj.GetComponent<Button>();

        var labelObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(buttonObj.transform, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObj.GetComponent<Text>();
        label.text = text;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.resizeTextForBestFit = true;

        return btn;
    }

    private SkinEditorUIController.UIHandles CreateUI(Transform parent)
    {
        var handles = new SkinEditorUIController.UIHandles();
        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject scroll = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        var scrollRect = scroll.GetComponent<RectTransform>();
        scrollRect.SetParent(parent, false);
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(20, 20);
        scrollRect.offsetMax = new Vector2(-20, -80);
        scroll.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(scroll.transform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(viewport.transform, false);
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = new Vector2(0, 0);
        contentRect.offsetMax = new Vector2(0, 0);
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 10f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollComponent = scroll.GetComponent<ScrollRect>();
        scrollComponent.content = contentRect;
        scrollComponent.viewport = viewportRect;
        scrollComponent.horizontal = false;

        AddHeader(content.transform, "Ball", font);
        handles.BallColorSliders = CreateColorPickers(content.transform, font, "Ball Color");
        handles.BallSizeSlider = CreateSlider(content.transform, font, "Ball Size", 0.5f, 2.5f);
        handles.BallPreview = CreatePreview(content.transform, font, "Ball Preview");

        AddHeader(content.transform, "Background", font);
        handles.BackgroundColorSliders = CreateColorPickers(content.transform, font, "Background Color");
        handles.BackgroundPreview = CreatePreview(content.transform, font, "Background Preview");
        handles.UseImageToggle = CreateToggle(content.transform, font, "Use background image");
        handles.BackgroundDropdown = CreateDropdown(content.transform, font, "Background image");
        handles.MobilePickerButton = CreateMiniButton(content.transform, font, "Pick mobile image");

        AddHeader(content.transform, "Save & Apply", font);
        handles.SkinNameInput = CreateInputField(content.transform, font, "Skin name");
        handles.SaveButton = CreateMiniButton(content.transform, font, "Save skin");
        handles.SkinsListParent = CreateList(content.transform, font, "Saved skins");

        var footer = new GameObject("Footer", typeof(RectTransform), typeof(Text));
        var footerRect = footer.GetComponent<RectTransform>();
        footerRect.SetParent(parent, false);
        footerRect.anchorMin = new Vector2(0, 0);
        footerRect.anchorMax = new Vector2(1, 0);
        footerRect.sizeDelta = new Vector2(0, 60);
        footerRect.anchoredPosition = new Vector2(0, 20);
        var footerText = footer.GetComponent<Text>();
        footerText.font = font;
        footerText.alignment = TextAnchor.MiddleCenter;
        footerText.color = Color.white;
        footerText.text = "Tap anywhere to close";
        handles.FooterLabel = footerText;

        return handles;
    }

    private void AddHeader(Transform parent, string title, Font font)
    {
        var header = new GameObject(title + "Header", typeof(RectTransform), typeof(Text));
        var rect = header.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0, 50);
        var txt = header.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = 28;
        txt.color = Color.white;
        txt.text = title;
    }

    private Slider[] CreateColorPickers(Transform parent, Font font, string label)
    {
        var container = new GameObject(label, typeof(RectTransform), typeof(VerticalLayoutGroup));
        var rect = container.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        var layout = container.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 4f;

        var sliders = new List<Slider>();
        foreach (var channel in new[] { "R", "G", "B" })
        {
            sliders.Add(CreateSlider(container.transform, font, $"{label} {channel}", 0f, 1f));
        }

        return sliders.ToArray();
    }

    private Slider CreateSlider(Transform parent, Font font, string label, float min, float max)
    {
        var sliderObj = new GameObject(label, typeof(RectTransform));
        var rect = sliderObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0, 60);

        var textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.SetParent(sliderObj.transform, false);
        textRect.anchorMin = new Vector2(0, 0.5f);
        textRect.anchorMax = new Vector2(0, 0.5f);
        textRect.sizeDelta = new Vector2(240, 40);
        textRect.anchoredPosition = new Vector2(120, 0);

        var txt = textObj.GetComponent<Text>();
        txt.font = font;
        txt.color = Color.white;
        txt.text = label;

        var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
        var sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.SetParent(sliderObj.transform, false);
        sliderRect.anchorMin = new Vector2(0, 0.5f);
        sliderRect.anchorMax = new Vector2(1, 0.5f);
        sliderRect.offsetMin = new Vector2(260, -15);
        sliderRect.offsetMax = new Vector2(-20, 15);

        var background = sliderGO.GetComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.SetParent(sliderGO.transform, false);
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(10, 0);
        fillAreaRect.offsetMax = new Vector2(-10, 0);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.SetParent(fillArea.transform, false);
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.4f, 0.7f, 1f, 0.8f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.SetParent(sliderGO.transform, false);
        handleRect.sizeDelta = new Vector2(20, 30);
        handle.GetComponent<Image>().color = Color.white;

        var slider = sliderGO.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = min;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();

        return slider;
    }

    private Image CreatePreview(Transform parent, Font font, string label)
    {
        var previewObj = new GameObject(label, typeof(RectTransform), typeof(Image));
        var rect = previewObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0, 140);

        var txtObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.SetParent(previewObj.transform, false);
        txtRect.anchorMin = new Vector2(0, 1);
        txtRect.anchorMax = new Vector2(1, 1);
        txtRect.sizeDelta = new Vector2(0, 40);
        txtRect.anchoredPosition = new Vector2(0, -20);
        var txt = txtObj.GetComponent<Text>();
        txt.font = font;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color = Color.white;
        txt.text = label;

        var img = previewObj.GetComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;

        return img;
    }

    private Toggle CreateToggle(Transform parent, Font font, string label)
    {
        var toggleObj = new GameObject(label, typeof(RectTransform), typeof(Toggle));
        var rect = toggleObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0, 60);

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.SetParent(toggleObj.transform, false);
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.sizeDelta = new Vector2(30, 30);
        bgRect.anchoredPosition = new Vector2(20, 0);
        var bgImage = background.GetComponent<Image>();
        bgImage.color = Color.white;

        var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        var checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.SetParent(background.transform, false);
        checkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;
        checkmark.GetComponent<Image>().color = Color.green;

        var labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(toggleObj.transform, false);
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(1, 0.5f);
        labelRect.offsetMin = new Vector2(60, -20);
        labelRect.offsetMax = new Vector2(-20, 20);
        var txt = labelObj.GetComponent<Text>();
        txt.font = font;
        txt.color = Color.white;
        txt.text = label;

        var toggle = toggleObj.GetComponent<Toggle>();
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.isOn = false;

        return toggle;
    }

    private Dropdown CreateDropdown(Transform parent, Font font, string label)
    {
        var container = new GameObject(label, typeof(RectTransform));
        var rect = container.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0, 80);

        var labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(container.transform, false);
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(0, 0.5f);
        labelRect.sizeDelta = new Vector2(260, 40);
        labelRect.anchoredPosition = new Vector2(130, 0);
        var txt = labelObj.GetComponent<Text>();
        txt.font = font;
        txt.color = Color.white;
        txt.text = label;

        var dropdownObj = new GameObject("Dropdown", typeof(RectTransform), typeof(Dropdown), typeof(Image));
        var dropdownRect = dropdownObj.GetComponent<RectTransform>();
        dropdownRect.SetParent(container.transform, false);
        dropdownRect.anchorMin = new Vector2(0, 0.5f);
        dropdownRect.anchorMax = new Vector2(1, 0.5f);
        dropdownRect.offsetMin = new Vector2(260, -20);
        dropdownRect.offsetMax = new Vector2(-20, 20);

        var image = dropdownObj.GetComponent<Image>();
        image.color = new Color(1, 1, 1, 0.9f);

        var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var templateRect = template.GetComponent<RectTransform>();
        templateRect.SetParent(dropdownObj.transform, false);
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.sizeDelta = new Vector2(0, 150);
        template.SetActive(false);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(template.transform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(viewport.transform, false);
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;

        var item = CreateDropdownItem(content.transform, font);

        var scrollRect = template.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;

        var dropdown = dropdownObj.GetComponent<Dropdown>();
        dropdown.targetGraphic = image;
        dropdown.template = templateRect;
        dropdown.captionText = item.caption;
        dropdown.itemText = item.item;
        dropdown.captionText.text = "None";
        dropdown.options = new List<Dropdown.OptionData>();

        return dropdown;
    }

    private (Text caption, Text item) CreateDropdownItem(Transform parent, Font font)
    {
        var item = new GameObject("Item", typeof(RectTransform));
        var itemRect = item.GetComponent<RectTransform>();
        itemRect.SetParent(parent, false);
        itemRect.anchorMin = new Vector2(0, 1);
        itemRect.anchorMax = new Vector2(1, 1);
        itemRect.sizeDelta = new Vector2(0, 30);

        var toggleObj = new GameObject("Item Toggle", typeof(RectTransform), typeof(Toggle));
        var toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.SetParent(item.transform, false);
        toggleRect.anchorMin = new Vector2(0, 0);
        toggleRect.anchorMax = new Vector2(1, 1);
        toggleRect.offsetMin = Vector2.zero;
        toggleRect.offsetMax = Vector2.zero;

        var background = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.SetParent(toggleObj.transform, false);
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(1, 1, 1, 0.1f);

        var checkmark = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
        var checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.SetParent(background.transform, false);
        checkRect.anchorMin = new Vector2(0, 0);
        checkRect.anchorMax = new Vector2(0, 1);
        checkRect.sizeDelta = new Vector2(20, 0);
        checkRect.anchoredPosition = new Vector2(10, 0);
        var checkImage = checkmark.GetComponent<Image>();
        checkImage.color = Color.green;

        var label = new GameObject("Item Label", typeof(RectTransform), typeof(Text));
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.SetParent(background.transform, false);
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(30, 0);
        labelRect.offsetMax = new Vector2(-10, 0);
        var labelText = label.GetComponent<Text>();
        labelText.font = font;
        labelText.color = Color.black;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.text = "Option";

        var toggle = toggleObj.GetComponent<Toggle>();
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;

        return (labelText, labelText);
    }

    private Button CreateMiniButton(Transform parent, Font font, string label)
    {
        var btnObj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = btnObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0, 70);

        btnObj.GetComponent<Image>().color = new Color(1, 1, 1, 0.9f);

        var textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.SetParent(btnObj.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var txt = textObj.GetComponent<Text>();
        txt.font = font;
        txt.color = Color.black;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = label;

        return btnObj.GetComponent<Button>();
    }

    private InputField CreateInputField(Transform parent, Font font, string label)
    {
        var container = new GameObject(label, typeof(RectTransform));
        var rect = container.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0, 80);

        var labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var lRect = labelObj.GetComponent<RectTransform>();
        lRect.SetParent(container.transform, false);
        lRect.anchorMin = new Vector2(0, 0.5f);
        lRect.anchorMax = new Vector2(0, 0.5f);
        lRect.sizeDelta = new Vector2(260, 40);
        lRect.anchoredPosition = new Vector2(130, 0);
        var txt = labelObj.GetComponent<Text>();
        txt.font = font;
        txt.color = Color.white;
        txt.text = label;

        var inputObj = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
        var inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.SetParent(container.transform, false);
        inputRect.anchorMin = new Vector2(0, 0.5f);
        inputRect.anchorMax = new Vector2(1, 0.5f);
        inputRect.offsetMin = new Vector2(260, -25);
        inputRect.offsetMax = new Vector2(-20, 25);

        inputObj.GetComponent<Image>().color = new Color(1, 1, 1, 0.9f);

        var textArea = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var textRect = textArea.GetComponent<RectTransform>();
        textRect.SetParent(inputObj.transform, false);
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 6);
        textRect.offsetMax = new Vector2(-10, -6);
        var inputText = textArea.GetComponent<Text>();
        inputText.font = font;
        inputText.color = Color.black;
        inputText.supportRichText = false;

        var placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        var placeRect = placeholderObj.GetComponent<RectTransform>();
        placeRect.SetParent(inputObj.transform, false);
        placeRect.anchorMin = new Vector2(0, 0);
        placeRect.anchorMax = new Vector2(1, 1);
        placeRect.offsetMin = new Vector2(10, 6);
        placeRect.offsetMax = new Vector2(-10, -6);
        var placeholderText = placeholderObj.GetComponent<Text>();
        placeholderText.font = font;
        placeholderText.color = new Color(0, 0, 0, 0.5f);
        placeholderText.text = "Skin name";

        var input = inputObj.GetComponent<InputField>();
        input.textComponent = inputText;
        input.placeholder = placeholderText;
        input.text = string.Empty;

        return input;
    }

    private Transform CreateList(Transform parent, Font font, string label)
    {
        var listObj = new GameObject(label, typeof(RectTransform), typeof(VerticalLayoutGroup));
        var rect = listObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0, 200);
        var layout = listObj.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 6f;
        var header = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var hRect = header.GetComponent<RectTransform>();
        hRect.SetParent(listObj.transform, false);
        hRect.sizeDelta = new Vector2(0, 40);
        var txt = header.GetComponent<Text>();
        txt.font = font;
        txt.color = Color.white;
        txt.text = label;

        return listObj.transform;
    }
}

public class SkinEditorUIController : MonoBehaviour
{
    public struct UIHandles
    {
        public Slider[] BallColorSliders;
        public Slider BallSizeSlider;
        public Image BallPreview;
        public Slider[] BackgroundColorSliders;
        public Image BackgroundPreview;
        public Toggle UseImageToggle;
        public Dropdown BackgroundDropdown;
        public Button MobilePickerButton;
        public InputField SkinNameInput;
        public Button SaveButton;
        public Transform SkinsListParent;
        public Text FooterLabel;
    }

    private UIHandles handles;
    private Button toggleButton;
    private Image sharedBackground;
    private SkinData workingCopy;

    public void Initialize(UIHandles handles, Button toggleButton, Image backgroundImage)
    {
        this.handles = handles;
        this.toggleButton = toggleButton;
        sharedBackground = backgroundImage;

        workingCopy = SkinManager.CurrentSkin.Clone();

        toggleButton.onClick.AddListener(TogglePanel);
        handles.FooterLabel.text = "Tap to close skin editor";
        GetComponent<Image>().raycastTarget = true;

        BindColorSliders(handles.BallColorSliders, workingCopy.BallColor, OnBallColorChanged);
        BindColorSliders(handles.BackgroundColorSliders, workingCopy.BackgroundColor, OnBackgroundColorChanged);
        handles.BallSizeSlider.value = workingCopy.BallSize;
        handles.BallSizeSlider.onValueChanged.AddListener(v => { workingCopy.BallSize = v; UpdatePreviews(); });
        handles.UseImageToggle.isOn = workingCopy.UseBackgroundImage;
        handles.UseImageToggle.onValueChanged.AddListener(OnUseImageToggled);

        PopulateBackgroundDropdown();
        handles.BackgroundDropdown.onValueChanged.AddListener(OnBackgroundDropdownChanged);
        handles.MobilePickerButton.onClick.AddListener(OnMobilePickRequested);

        handles.SkinNameInput.text = workingCopy.Name;
        handles.SaveButton.onClick.AddListener(SaveSkin);

        RefreshSavedSkins();
        UpdatePreviews();
    }

    private void TogglePanel()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void BindColorSliders(Slider[] sliders, Color initial, Action<Color> onChanged)
    {
        if (sliders == null || sliders.Length < 3) return;
        sliders[0].value = initial.r;
        sliders[1].value = initial.g;
        sliders[2].value = initial.b;

        sliders[0].onValueChanged.AddListener(_ => onChanged(CollectColor(sliders)));
        sliders[1].onValueChanged.AddListener(_ => onChanged(CollectColor(sliders)));
        sliders[2].onValueChanged.AddListener(_ => onChanged(CollectColor(sliders)));
    }

    private Color CollectColor(Slider[] sliders)
    {
        return new Color(sliders[0].value, sliders[1].value, sliders[2].value, 1f);
    }

    private void OnBallColorChanged(Color color)
    {
        workingCopy.BallColor = color;
        UpdatePreviews();
    }

    private void OnBackgroundColorChanged(Color color)
    {
        workingCopy.BackgroundColor = color;
        UpdatePreviews();
    }

    private void OnUseImageToggled(bool enabled)
    {
        workingCopy.UseBackgroundImage = enabled;
        UpdatePreviews();
    }

    private void OnBackgroundDropdownChanged(int index)
    {
        if (index < 0 || index >= handles.BackgroundDropdown.options.Count) return;
        var option = handles.BackgroundDropdown.options[index];
        workingCopy.BackgroundSpriteName = option.text == "None" ? string.Empty : option.text;
        UpdatePreviews();
    }

    private void OnMobilePickRequested()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.GET_CONTENT"))
            {
                intent.Call<AndroidJavaObject>("setType", "image/*");
                activity.Call("startActivity", intent);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Image picker failed: {ex.Message}");
        }
#else
        Debug.Log("Image picker is available on mobile builds.");
#endif
    }

    private void SaveSkin()
    {
        SkinManager.SaveSkin(handles.SkinNameInput.text, workingCopy);
        RefreshSavedSkins();
    }

    private void RefreshSavedSkins()
    {
        foreach (Transform child in handles.SkinsListParent)
        {
            if (child.gameObject.name != "Label")
            {
                Destroy(child.gameObject);
            }
        }

        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        foreach (var skin in SkinManager.Skins)
        {
            var btnObj = new GameObject(skin.Name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = btnObj.GetComponent<RectTransform>();
            rect.SetParent(handles.SkinsListParent, false);
            rect.sizeDelta = new Vector2(0, 60);
            btnObj.GetComponent<Image>().color = new Color(1, 1, 1, 0.9f);
            var textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var tRect = textObj.GetComponent<RectTransform>();
            tRect.SetParent(btnObj.transform, false);
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var txt = textObj.GetComponent<Text>();
            txt.font = font;
            txt.color = Color.black;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = skin.Name;

            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => { SkinManager.ApplySkin(skin.Name); workingCopy = SkinManager.CurrentSkin.Clone(); SyncFromWorkingCopy(); });
        }
    }

    private void SyncFromWorkingCopy()
    {
        handles.BallColorSliders[0].SetValueWithoutNotify(workingCopy.BallColor.r);
        handles.BallColorSliders[1].SetValueWithoutNotify(workingCopy.BallColor.g);
        handles.BallColorSliders[2].SetValueWithoutNotify(workingCopy.BallColor.b);
        handles.BackgroundColorSliders[0].SetValueWithoutNotify(workingCopy.BackgroundColor.r);
        handles.BackgroundColorSliders[1].SetValueWithoutNotify(workingCopy.BackgroundColor.g);
        handles.BackgroundColorSliders[2].SetValueWithoutNotify(workingCopy.BackgroundColor.b);
        handles.BallSizeSlider.SetValueWithoutNotify(workingCopy.BallSize);
        handles.UseImageToggle.SetIsOnWithoutNotify(workingCopy.UseBackgroundImage);

        var index = handles.BackgroundDropdown.options.FindIndex(o => o.text == workingCopy.BackgroundSpriteName);
        if (index < 0) index = 0;
        handles.BackgroundDropdown.SetValueWithoutNotify(index);

        handles.SkinNameInput.text = workingCopy.Name;
        UpdatePreviews();
    }

    private void UpdatePreviews()
    {
        handles.BallPreview.color = workingCopy.BallColor;
        handles.BallPreview.transform.localScale = Vector3.one * workingCopy.BallSize;

        handles.BackgroundPreview.color = workingCopy.BackgroundColor;
        handles.BackgroundPreview.sprite = null;
        handles.BackgroundPreview.enabled = true;

        if (workingCopy.UseBackgroundImage)
        {
            var sprite = SkinManager.LoadBackgroundSprite(workingCopy.BackgroundSpriteName);
            handles.BackgroundPreview.sprite = sprite;
            handles.BackgroundPreview.color = sprite == null ? workingCopy.BackgroundColor : Color.white;
            handles.BackgroundPreview.preserveAspect = true;
        }

        if (sharedBackground != null)
        {
            if (workingCopy.UseBackgroundImage)
            {
                var sprite = SkinManager.LoadBackgroundSprite(workingCopy.BackgroundSpriteName);
                sharedBackground.sprite = sprite ?? sharedBackground.sprite;
                sharedBackground.color = sprite == null ? workingCopy.BackgroundColor : Color.white;
            }
            else
            {
                sharedBackground.color = workingCopy.BackgroundColor;
            }
        }

        SkinManager.UpdateWorkingCopy(workingCopy);
    }

    private void PopulateBackgroundDropdown()
    {
        var sprites = Resources.LoadAll<Sprite>("UI/HomeScreen");
        handles.BackgroundDropdown.options.Clear();
        handles.BackgroundDropdown.options.Add(new Dropdown.OptionData("None"));
        foreach (var sprite in sprites)
        {
            handles.BackgroundDropdown.options.Add(new Dropdown.OptionData(sprite.name));
        }

        var index = 0;
        if (!string.IsNullOrEmpty(workingCopy.BackgroundSpriteName))
        {
            index = handles.BackgroundDropdown.options.FindIndex(o => o.text == workingCopy.BackgroundSpriteName);
            if (index < 0) index = 0;
        }
        handles.BackgroundDropdown.value = index;
    }
}
