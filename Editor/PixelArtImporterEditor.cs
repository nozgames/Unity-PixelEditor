using System.Linq;
using UnityEditor;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
#endif

namespace NoZ.PA
{
    [CustomEditor(typeof(PixelArtImporter))]
    internal class PixelArtImporterEditor : ScriptedImporterEditor
    {
        public override bool showImportedObject => false;

        private PAFile _file;
        private string[] _animationNames;
        private PixelArtImporter _importer;
        private SerializedProperty layerMode;
        private SerializedProperty defaultAnimationIndex;

        public override void OnEnable()
        {
            base.OnEnable();

            layerMode = serializedObject.FindProperty("layerMode");
            defaultAnimationIndex = serializedObject.FindProperty("defaultAnimationIndex");

            _file = PAFile.Load((target as PixelArtImporter).assetPath);
            _animationNames = _file.animations.Select(a => a.name).ToArray();
        }

        public sealed override void OnInspectorGUI() 
        {
            var layerWrapModeEnum = (PixelArtImporter.LayerMode)layerMode.intValue;
            EditorGUI.BeginChangeCheck();
            layerWrapModeEnum = (PixelArtImporter.LayerMode)EditorGUILayout.EnumFlagsField("Layer Mode", layerWrapModeEnum);
            if(EditorGUI.EndChangeCheck())
                layerMode.intValue = (int)layerWrapModeEnum;

            defaultAnimationIndex.intValue = EditorGUILayout.Popup("Default Animation", defaultAnimationIndex.intValue, _animationNames);

            base.ApplyRevertGUI();
        }

        public override bool HasPreviewGUI()
        {
            return !serializedObject.isEditingMultipleObjects;
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
        }
    }
}
