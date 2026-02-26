using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Diagnostics;
using System.Collections.Generic;

public class EditGeneration : EditorWindow
{
    private EnumField _multiSceneField;
    private TextField _promptField;
    private EnumField _subjectTypeField;
    private Toggle _vrDataToggle;
    private Button _submitButton;

    private PopupField<string> _worldDropdown;
    private Toggle _worldNameChangeToggle;
    private TextField _currentWorldNameField;
    private TextField _newWorldNameField;

    [MenuItem("Generator/Edit")]
    private static void OpenWindow()
    {
        
        var window = GetWindow<EditGeneration>("Edit Generation");
        window.titleContent = new GUIContent("Edit");
        window.minSize = new Vector2(500, 300);
        //window.Show();
        //window.CreateGUI();
    }
    //private void OnEnable()
    //{
    //    CreateGUI();
    //}

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.Clear();

        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;

        Label header = new Label("Edit an existing a world");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        root.Add(header);

        var generationsDict = GenerationsLoader.LoadGenerations();
        UnityEngine.Debug.Log($"Generations dict: {generationsDict}");
        List<string> worldNames = new List<string>();

        foreach (var kvp in generationsDict)
        {
            worldNames.Add(kvp.Key);
        }

        if (worldNames.Count == 0)
            worldNames.Add("No Generations Found");

        _worldDropdown = new PopupField<string>(
            "World To Edit",
            worldNames,
            0
        );

        root.Add(_worldDropdown);

        _currentWorldNameField = new TextField("Current Name");
        _currentWorldNameField.value = _worldDropdown.value;
        _currentWorldNameField.SetEnabled(false);
        root.Add(_currentWorldNameField);

        

        // Edit World Name
        _worldNameChangeToggle = new Toggle("Change World Name");
        root.Add(_worldNameChangeToggle);

        _newWorldNameField = new TextField("World Name");
        _newWorldNameField.style.display = DisplayStyle.None;
        root.Add(_newWorldNameField);

        _worldDropdown.RegisterValueChangedCallback(evt =>
        {
            _currentWorldNameField.value = evt.newValue;
        });

        _worldNameChangeToggle.RegisterValueChangedCallback(evt =>
        {
            _newWorldNameField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            if (evt.newValue)
            {
                _newWorldNameField.value = _currentWorldNameField.value;
            }
            else
            {
                _newWorldNameField.value = string.Empty;
            }
        });


        // Multi-Scene Selector
        _multiSceneField = new EnumField("Multi-Scene Mode", MultiSceneMode.Auto);
        root.Add(_multiSceneField);

        // Prompt (multiline)
        _promptField = new TextField("Edit Prompt");
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
        string _outputWorldName = _worldNameChangeToggle.value == true ? _newWorldNameField.value : _currentWorldNameField.value;
        GeneratorRequest request = new GeneratorRequest
        {
            worldName = _currentWorldNameField.value,
            outputWorldName = _outputWorldName,
            multiSceneMode = (MultiSceneMode)_multiSceneField.value,
            prompt = _promptField.value,
            subjectType = (SubjectType)_subjectTypeField.value,
            useDataCollectionAssets =
                ((SubjectType)_subjectTypeField.value == SubjectType.VR)
                && _vrDataToggle.value
        };

        UnityEngine.Debug.Log("Generator Request Created:" + JsonUtility.ToJson(request, true));

        request.Send();

        GenerationsLoader.DumpGeneration(_outputWorldName, _promptField.value);
    }

    
}