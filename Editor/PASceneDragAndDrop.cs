using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace NoZ.PA
{
    [InitializeOnLoad]
    public static class PASceneDragAndDrop
    {
        static PASceneDragAndDrop()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnSceneGUI;            
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
            HierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        }

        private static readonly System.Type HierarchyWindowType;
        private static GameObject[] _dragObjects;
        private static int _hoveredInstance = 0;

        /// <summary>
        /// Returns true if the mouse is currently over the hierarchy window
        /// </summary>
        private static bool IsMouseOverHierarchyWindow =>
            EditorWindow.mouseOverWindow != null && EditorWindow.mouseOverWindow.GetType() == HierarchyWindowType;

        static void OnSceneGUI(SceneView sceneView) =>
            HandleSceneDrag(sceneView, Event.current, DragAndDrop.objectReferences, DragAndDrop.paths);

        static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            switch (Event.current.type)
            {
                // Track the hierarchy item the mouse is currently over by assuming that
                // whenever the mouse enters or leaves an item it will have to be redrawn
                // to handle the hover state change.
                case EventType.Repaint:
                    if (IsMouseOverHierarchyWindow)
                    {
                        if (selectionRect.Contains(Event.current.mousePosition))
                            _hoveredInstance = instanceID;
                        else if (_hoveredInstance == instanceID)
                            _hoveredInstance = 0;
                    }
                    else
                        _hoveredInstance = 0;

                    break;

                default:
                    HandleSceneDrag(null, Event.current, DragAndDrop.objectReferences, null);
                    break;
            }
        }

        private static void HandleSceneDrag(SceneView sceneView, Event evt, Object[] objects, string[] paths)
        {
            switch (evt.type)
            {
                case EventType.DragUpdated:
                {
                    // Start of a drag?
                    if (_dragObjects == null)
                        CreateDragObjects(objects);

                    // If we are dragging objects then position them in the scene view
                    if (_dragObjects != null)
                    {
                        DragAndDrop.AcceptDrag();
                        PositionDragObjectsInScene(sceneView, evt.mousePosition);
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        evt.Use();
                    }
                    break;
                }

                case EventType.DragPerform:
                {
                    if (_dragObjects != null)
                    {
                        // If the mouse is over a gameobject in the hierarchy window then set that object as the parent
                        if (IsMouseOverHierarchyWindow &&  _hoveredInstance != 0)
                        {
                            var hoverObject = EditorUtility.InstanceIDToObject(_hoveredInstance) as GameObject;
                            if (null != hoverObject)
                            {
                                foreach (var gameObject in _dragObjects)
                                {
                                    gameObject.transform.SetParent(hoverObject.transform);
                                    gameObject.transform.localPosition = Vector3.zero;
                                }
                            }
                        }

                        // Finish the drop operation by removing the hidden flag, generating a unique name, and adding undo support
                        foreach (var gameObject in _dragObjects)
                        {
                            gameObject.hideFlags = HideFlags.None;
                            gameObject.name = GameObjectUtility.GetUniqueNameForSibling(gameObject.transform.parent, gameObject.name.Substring(2));
                            Undo.RegisterCreatedObjectUndo(gameObject, "Create PixelArt");
                        }

                        // Select the newly created objects
                        Selection.objects = _dragObjects.ToArray();

                        CleanUp(false);
                        evt.Use();
                    }
                    break;
                }

                case EventType.DragExited:
                {
                    if (_dragObjects != null)
                    {
                        CleanUp(true);
                        evt.Use();
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Position our current list of drag objects within the scene based on the mouse position
        /// </summary>
        private static void PositionDragObjectsInScene (SceneView sceneView, Vector2 mousePosition)
        {
            if (sceneView == null || _dragObjects == null)
                return;

            var position = HandleUtility.GUIPointToWorldRay(mousePosition).GetPoint(10);
            
            if (sceneView.in2DMode)
            {
                position.z = 0f;
            }
            else
            {
                var hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(mousePosition));
                if (hit != null)
                    position = ((RaycastHit)hit).point;
            }

            // Set the object positions
            foreach (var gameObject in _dragObjects)
                gameObject.transform.position = position;
        }

        /// <summary>
        /// Create game objects from the given pixel art assets
        /// </summary>
        private static void CreateDragObjects (Object[] objects)
        {
            // Before we allocate an array lets make sure there is at least on pixel art in the list of objects
            if (!objects.Where(o => AssetDatabase.Contains(o) && o is PixelArt).Any())
                return;

            // Create the list of drag objects
            _dragObjects = objects.Where(o => (o as PixelArt) != null).Cast<PixelArt>().Select(pixelArt =>
            {
                var gameObject = new GameObject();
                gameObject.name = $"__{pixelArt.name}";
                gameObject.hideFlags = HideFlags.HideAndDontSave;

                var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = pixelArt.defaultAnimation.GetFrameAt(0).sprite;

                var animator = gameObject.AddComponent<PixelArtAnimator>();
                animator.PixelArt = pixelArt;
                animator.layers = new SpriteRenderer[] { spriteRenderer };
                return gameObject;

            }).ToArray();
        }

        /// <summary>
        /// Cleanup after a drag operation and optionally destroy the game objects being used
        /// </summary>
        private static void CleanUp(bool deleteTempSceneObject)
        {
            if (_dragObjects == null)
                return;

            if (deleteTempSceneObject)
                foreach (var gameObject in _dragObjects)
                    Object.DestroyImmediate(gameObject, false);

            _dragObjects = null;
        }
    }
}
