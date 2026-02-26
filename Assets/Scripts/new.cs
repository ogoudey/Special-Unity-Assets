using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Diagnostics;
public enum MultiSceneMode
{
    Yes,
    No,
    Auto
}

public enum SubjectType
{
    None,
    Player,
    VR
}

public class NewGeneration : EditorWindow
{
    private TextField _worldNameField;
    private EnumField _multiSceneField;
    private TextField _promptField;
    private EnumField _subjectTypeField;
    private Toggle _vrDataToggle;
    private Button _submitButton;

    [MenuItem("Generator/New")]
    private static void OpenWindow()
    {
        var window = GetWindow<NewGeneration>("New Generation");
        window.titleContent = new GUIContent("New");
        window.minSize = new Vector2(500, 300);
    }
    private void OnEnable()
    {
        CreateGUI();
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.Clear();

        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;

        Label header = new Label("Generate a New World");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        root.Add(header);

        // World Name
        _worldNameField = new TextField("World Name");
        _worldNameField.value = "NewWorld";
        root.Add(_worldNameField);

        // Multi-Scene Selector
        _multiSceneField = new EnumField("Multi-Scene Mode", MultiSceneMode.Auto);
        root.Add(_multiSceneField);

        // Prompt (multiline)
        _promptField = new TextField("Prompt");
        _promptField.multiline = true;
        _promptField.style.height = 80;
        root.Add(_promptField);

        // Subject Type
        _subjectTypeField = new EnumField("Subject Type", SubjectType.None);
        root.Add(_subjectTypeField);

        // VR Data Collection Toggle (hidden by default)
        _vrDataToggle = new Toggle("Use Data Collection Assets");
        _vrDataToggle.style.display = DisplayStyle.None;
        root.Add(_vrDataToggle);

        // Show/hide VR toggle depending on subject type
        _subjectTypeField.RegisterValueChangedCallback(evt =>
        {
            SubjectType selected = (SubjectType)evt.newValue;
            _vrDataToggle.style.display =
                selected == SubjectType.VR ? DisplayStyle.Flex : DisplayStyle.None;
        });

        // Submit Button
        _submitButton = new Button(OnSubmitClicked)
        {
            text = "Generate"
        };

        _submitButton.style.marginTop = 10;
        root.Add(_submitButton);
    }

    private void OnSubmitClicked()
    {   
        GeneratorRequest request = new GeneratorRequest
        {
            worldName = _worldNameField.value,
            outputWorldName = _worldNameField.value,
            multiSceneMode = (MultiSceneMode)_multiSceneField.value,
            prompt = _promptField.value,
            subjectType = (SubjectType)_subjectTypeField.value,
            useDataCollectionAssets =
                ((SubjectType)_subjectTypeField.value == SubjectType.VR)
                && _vrDataToggle.value
        };

        UnityEngine.Debug.Log("Generator Request Created:" + JsonUtility.ToJson(request, true));

        request.Send();
        
        GenerationsLoader.DumpGeneration(_worldNameField.value, _promptField.value);

    }
}
