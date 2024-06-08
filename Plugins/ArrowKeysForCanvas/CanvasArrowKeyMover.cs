using UnityEngine;
using UnityEditor;

namespace ArrowKeysForCanvas
{
    /// <summary>
    /// Allows moving selected objects in the Scene View using arrow keys.
    /// </summary>
    [InitializeOnLoad]
    public class CanvasArrowKeyMover
    {
        #region Variables

        // Speed at which the object will move when arrow keys are pressed
        private static float _moveSpeed = 0.1f;

        // Initial position and size of the overlay window
        private static Rect _windowRect = new Rect(4, 645, 200, 40);

        // Relative position of the panel
        private static Vector2 _relativePosition = new Vector2(4, 1);

        // Variables to manage the speed adjustment
        private static float _speedAdjustmentRate = 0.01f;
        private static float _holdThreshold = 0.5f;
        private static float _accelerationRate = 0.1f;
        private static bool _isHoldingPlus = false;
        private static bool _isHoldingMinus = false;
        private static float _holdStartTime = 0f;

        #endregion

        #region Properties

        // Property to get and set moveSpeed with clamping
        public static float moveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = Mathf.Clamp(value, 0.01f, 100f);
        }

        #endregion

        #region Initialization

        static CanvasArrowKeyMover()
        {
            // Subscribe to the SceneView's duringSceneGui event to call OnSceneGUI method
            SceneView.duringSceneGui += OnSceneGUI;
        }

        #endregion

        #region SceneView Event Handling

        // Method called during the Scene View GUI rendering
        private static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            if (e != null && Selection.activeGameObject != null && SceneView.currentDrawingSceneView.in2DMode)
            {
                if (IsChildOfCanvas(Selection.activeGameObject))
                {
                    if (e.type == EventType.KeyDown)
                    {
                        HandleArrowKeyMovement(e);
                        HandleSpeedAdjustmentStart(e);
                    }
                    else if (e.type == EventType.KeyUp)
                    {
                        HandleSpeedAdjustmentEnd(e);
                    }

                    if (_isHoldingPlus || _isHoldingMinus)
                    {
                        HandleSpeedAdjustmentHold();
                    }
                }
            }

            // Draw the move speed panel if conditions are met
            if (Selection.activeGameObject != null && SceneView.currentDrawingSceneView.in2DMode && IsChildOfCanvas(Selection.activeGameObject))
            {
                DrawMoveSpeedPanel(sceneView.position);
            }
        }

        #endregion

        #region Input Handling

        // Handle arrow key movement
        private static void HandleArrowKeyMovement(Event e)
        {
            GameObject selectedObject = Selection.activeGameObject;

            if (IsChildOfCanvas(selectedObject))
            {
                Vector2 moveDirection = Vector2.zero;

                switch (e.keyCode)
                {
                    case KeyCode.UpArrow:
                        moveDirection = Vector2.up;
                        break;
                    case KeyCode.DownArrow:
                        moveDirection = Vector2.down;
                        break;
                    case KeyCode.LeftArrow:
                        moveDirection = Vector2.left;
                        break;
                    case KeyCode.RightArrow:
                        moveDirection = Vector2.right;
                        break;
                    default:
                        return;
                }

                moveDirection *= moveSpeed;

                Undo.RecordObject(selectedObject.transform, "Move Object with Arrow Keys");
                selectedObject.transform.position += new Vector3(moveDirection.x, moveDirection.y, 0);

                e.Use();
            }
        }

        // Handle the start of speed adjustment
        private static void HandleSpeedAdjustmentStart(Event e)
        {
            bool isPlusKey = e.keyCode == KeyCode.Plus || e.keyCode == KeyCode.KeypadPlus;
            bool isMinusKey = e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus;

            if (isPlusKey || isMinusKey)
            {
                _holdStartTime = Time.realtimeSinceStartup;

                if (isPlusKey)
                {
                    _isHoldingPlus = true;
                    moveSpeed += _speedAdjustmentRate;
                }
                else if (isMinusKey)
                {
                    _isHoldingMinus = true;
                    moveSpeed = Mathf.Max(0, moveSpeed - _speedAdjustmentRate); // Ensure move speed doesn't go negative
                }

                SceneView.RepaintAll();
                e.Use();
            }
        }

        // Handle the end of speed adjustment
        private static void HandleSpeedAdjustmentEnd(Event e)
        {
            if (e.keyCode == KeyCode.Plus || e.keyCode == KeyCode.KeypadPlus)
            {
                _isHoldingPlus = false;
            }
            else if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus)
            {
                _isHoldingMinus = false;
            }

            SceneView.RepaintAll();
            e.Use();
        }

        // Handle holding down the speed adjustment keys
        private static void HandleSpeedAdjustmentHold()
        {
            float currentTime = Time.realtimeSinceStartup;
            float elapsedTime = currentTime - _holdStartTime;

            if (elapsedTime > _holdThreshold)
            {
                float changeAmount = _accelerationRate * (elapsedTime - _holdThreshold);
                if (_isHoldingPlus)
                {
                    moveSpeed += changeAmount * Time.deltaTime;
                }
                else if (_isHoldingMinus)
                {
                    moveSpeed = Mathf.Max(0, moveSpeed - changeAmount * Time.deltaTime); // Ensure move speed doesn't go negative
                }

                SceneView.RepaintAll();
            }
        }

        #endregion

        #region Utility Methods

        // Check if a GameObject is a child of a Canvas
        private static bool IsChildOfCanvas(GameObject obj)
        {
            Transform current = obj.transform;
            while (current != null)
            {
                if (current.GetComponent<Canvas>() != null)
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
        }

        #endregion

        #region GUI Handling

        // Draw the move speed panel in the Scene View
        private static void DrawMoveSpeedPanel(Rect sceneViewRect)
        {
            Handles.BeginGUI();

            _windowRect.x = _relativePosition.x;
            _windowRect.y = sceneViewRect.height - _windowRect.height - _relativePosition.y;

            _windowRect = GUILayout.Window(123456, _windowRect, DrawWindowContents, "Arrow Key Move Speed", "window");

            _windowRect.x = Mathf.Clamp(_windowRect.x, 4, sceneViewRect.width - _windowRect.width - 4);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 4, (sceneViewRect.height - 25) - _windowRect.height - 4);

            _relativePosition = new Vector2(_windowRect.x, sceneViewRect.height - _windowRect.y - _windowRect.height);

            Handles.EndGUI();
        }

        // Draw the contents of the move speed window
        private static void DrawWindowContents(int windowID)
        {
            EditorGUI.BeginChangeCheck();
            moveSpeed = EditorGUILayout.FloatField("Move Speed", moveSpeed);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            GUI.DragWindow(new Rect(0, 0, _windowRect.width, _windowRect.height));
        }

        #endregion
    }
}
