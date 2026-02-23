using UnityEngine;
using UnityEngine.UIElements;
using AshDev.Focus;
using System.Reflection;

public class TaskDragManipulator : PointerManipulator
{
    private ITaskManager window;
    private VisualElement ghost;
    private bool isDragging;
    private bool hasMoved;
    private VisualElement root;
    private Vector2 startPosition;
    private const float DRAG_THRESHOLD = 5f;

    public TaskDragManipulator(ITaskManager w)
    {
        this.window = w;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    // --- BẮT ĐẦU KÉO ---
    private void OnPointerDown(PointerDownEvent evt)
    {
        // Lấy dữ liệu từ thẻ đang được nhấn
        var task = target.userData as TaskData;
        if (task == null) return;

        isDragging = true;
        hasMoved = false;
        startPosition = evt.position;
        root = target.panel.visualTree;

        // Bắt giữ con trỏ chuột (để chuột ra khỏi vùng thẻ vẫn nhận sự kiện)
        target.CapturePointer(evt.pointerId);
    }

    // --- ĐANG DI CHUYỂN ---
    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isDragging) return;

        // Check nếu di chuyển đủ xa mới tạo ghost
        if (!hasMoved)
        {
            float distance = Vector2.Distance(evt.position, startPosition);
            if (distance > DRAG_THRESHOLD)
            {
                hasMoved = true;
                var task = target.userData as TaskData;
                if (task != null)
                {
                    CreateGhost(task);
                    UpdateGhost(evt.position);
                    target.style.opacity = 0.3f;
                }
            }
        }
        else
        {
            UpdateGhost(evt.position);
        }
    }

    // --- THẢ CHUỘT (KẾT THÚC) ---
    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!isDragging) return;

        target.ReleasePointer(evt.pointerId);

        // Chỉ move task nếu thực sự đã drag
        if (hasMoved)
        {
            // Logic quan trọng: "Bắn tia" xem chuột đang nằm trên cái gì
            VisualElement picked = root.panel.Pick(evt.position);

            // Tìm xem cái đó có nằm trong Cột (Column) nào không
            var colElement = FindParentColumn(picked);

            if (colElement != null)
            {
                // Lấy dữ liệu cột đích và task hiện tại
                var targetCol = colElement.userData as TaskColumn;
                var myTask = target.userData as TaskData;

                if (targetCol != null && myTask != null)
                {
                    // Gọi ngược lại hàm MoveTask bên FocusWindow để xử lý dữ liệu
                    window.MoveTask(myTask, targetCol);
                }
            }
        }

        StopDrag();
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (isDragging) StopDrag();
    }

    // --- CÁC HÀM PHỤ TRỢ ---

    private void StopDrag()
    {
        isDragging = false;
        hasMoved = false;
        target.style.opacity = 1f; // Trả lại độ đậm cho thẻ gốc
        if (ghost != null)
        {
            ghost.RemoveFromHierarchy();
            ghost = null;
        }
    }

    private void CreateGhost(TaskData task)
    {
        ghost = new VisualElement();
        ghost.style.position = Position.Absolute;
        ghost.style.width = target.resolvedStyle.width;
        ghost.style.height = target.resolvedStyle.height;
        ghost.style.backgroundColor = new Color(0.15f, 0.15f, 0.17f, 0.9f); // Màu nền tối

        // Bo góc
        ghost.style.borderTopLeftRadius = 8;
        ghost.style.borderBottomLeftRadius = 8;
        ghost.style.borderTopRightRadius = 8;
        ghost.style.borderBottomRightRadius = 8;

        // Viền tím (Style thủ công từng cạnh)
        float borderWidth = 1f;
        ghost.style.borderTopWidth = borderWidth;
        ghost.style.borderBottomWidth = borderWidth;
        ghost.style.borderLeftWidth = borderWidth;
        ghost.style.borderRightWidth = borderWidth;

        Color borderColor = new Color(0.4f, 0.4f, 1f); // Tím xanh
        ghost.style.borderTopColor = borderColor;
        ghost.style.borderBottomColor = borderColor;
        ghost.style.borderLeftColor = borderColor;
        ghost.style.borderRightColor = borderColor;

        ghost.style.rotate = new StyleRotate(new Rotate(3)); // Nghiêng 3 độ cho đẹp
        ghost.pickingMode = PickingMode.Ignore; // QUAN TRỌNG: Để chuột nhìn xuyên qua ghost

        var lbl = new Label(task.title);
        if (string.IsNullOrEmpty(task.title))
        {
            var titleProp = task.GetType().GetProperty("Title");
            if (titleProp != null)
                lbl.text = titleProp.GetValue(task)?.ToString() ?? "";
        }
        lbl.style.color = Color.white;
        lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        lbl.style.paddingLeft = 12;
        lbl.style.paddingTop = 12;
        ghost.Add(lbl);

        root.Add(ghost);
    }

    private void UpdateGhost(Vector2 screenPos)
    {
        if (ghost == null) return;
        // Chuyển đổi tọa độ màn hình sang tọa độ local của panel
        Vector2 localPos = root.WorldToLocal(screenPos);
        ghost.style.left = localPos.x - 10;
        ghost.style.top = localPos.y - 10;
    }

    private VisualElement FindParentColumn(VisualElement element)
    {
        while (element != null)
        {
            // Tìm element cha nào có class "task-column"
            if (element.ClassListContains("task-column")) return element;
            element = element.parent;
        }

        return null;
    }
}