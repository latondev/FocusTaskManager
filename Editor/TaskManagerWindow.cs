using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;
using Object = UnityEngine.Object;

namespace AshDev.Focus
{
    public class TaskManagerWindow : EditorWindow, ITaskManager
    {
    private FocusBoardData boardData;
    private VisualElement root;
    private ScrollView boardScrollView;
    private VisualElement boardContainer;
    private VisualElement tagManagerTab;
    private ScrollView tagContentContainer;
    private VisualElement tabContentContainer;
    private VisualElement tabHeader;
    private enum TabType { TaskBoard, TagManager }
    private TabType currentTab = TabType.TaskBoard;
    private VisualElement dialogOverlay;
    private VisualElement columnNameDialogOverlay;
    private TextField titleInput;
    private TextField descInput;
    private VisualElement dropArea;
    private VisualElement attachedFilesContainer;
    private TaskData currentEditingTask;
    private TaskColumn currentEditingColumn;
    private List<Object> tempAttachments = new List<Object>();
        private bool isGridView = true;
        private PopupField<string> tagDropdown;
        private TextField tagManagerInput;
        private VisualElement tagManagerTagsContainer;
        private VisualElement tagsContainer;
        private List<string> tempTags = new List<string>();
        private TextField columnNameInput;
        private PopupField<TaskColumn> columnDropdown;

    [MenuItem("Tools/Focus Task Manager")]
    public static void ShowWindow()
    {
        TaskManagerWindow wnd = GetWindow<TaskManagerWindow>();
        wnd.titleContent = new GUIContent("Focus - Task Manager");
        wnd.minSize = new Vector2(800, 500);
    }

    public void CreateGUI()
    {
        root = rootVisualElement;
        LoadData();
        
        // Bạn nhớ tạo file USS và check đường dẫn này nhé
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Focus/UI/TaskBoard.uss");
        if (styleSheet != null) root.styleSheets.Add(styleSheet);

        BuildMainLayout();
        RefreshBoard();
    }

    private void LoadData()
    {
        // Đảm bảo thư mục tồn tại
        if (!System.IO.Directory.Exists("Assets/Editor/Focus/UI"))
            System.IO.Directory.CreateDirectory("Assets/Editor/Focus/UI");

        string path = "Assets/Editor/Focus/UI/TaskBoardData.asset";
        boardData = AssetDatabase.LoadAssetAtPath<FocusBoardData>(path);
        if (boardData == null)
        {
            boardData = ScriptableObject.CreateInstance<FocusBoardData>();
            boardData.columns.Add(new TaskColumn { title = "To Do" });
            boardData.columns.Add(new TaskColumn { title = "In Progress" });
            boardData.columns.Add(new TaskColumn { title = "Done" });
            AssetDatabase.CreateAsset(boardData, path);
            AssetDatabase.SaveAssets();
        }
    }

    private void BuildMainLayout()
    {
        root.Clear();
        // --- HEADER ---
        var header = new VisualElement();
        header.AddToClassList("header");
        var titleLabel = new Label("Focus - Task Management");
        titleLabel.AddToClassList("title");
        header.Add(titleLabel);
        
        var headerBtns = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
        var addTaskBtn = new Button(() => OpenCreateTaskDialog()) { text = "+ Add Task" };
        addTaskBtn.AddToClassList("primary-button");
        addTaskBtn.style.marginRight = 5;
        var addColBtn = new Button(OnAddColumnClicked) { text = "+ Add Column" };
        addColBtn.AddToClassList("secondary-button");
        addColBtn.style.marginRight = 5;
        var refreshBtn = new Button(RefreshBoard) { text = "Refresh" };
        refreshBtn.AddToClassList("secondary-button");

        headerBtns.Add(addTaskBtn);
        headerBtns.Add(addColBtn);
        headerBtns.Add(refreshBtn);
        header.Add(headerBtns);
        root.Add(header);

        // --- TAB HEADER ---
        tabHeader = new VisualElement();
        tabHeader.style.flexDirection = FlexDirection.Row;
        tabHeader.style.paddingTop = 10;
        tabHeader.style.paddingBottom = 10;
        tabHeader.style.paddingLeft = 10;
        tabHeader.style.paddingRight = 10;
        tabHeader.style.borderBottomWidth = 1;
        tabHeader.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f);

        var taskBoardTabBtn = new Button(() => SwitchTab(TabType.TaskBoard));
        taskBoardTabBtn.text = "Task Board";
        taskBoardTabBtn.style.flexGrow = 1;
        taskBoardTabBtn.AddToClassList("tab-button");
        taskBoardTabBtn.AddToClassList("tab-active");
        taskBoardTabBtn.userData = TabType.TaskBoard;

        var tagManagerTabBtn = new Button(() => SwitchTab(TabType.TagManager));
        tagManagerTabBtn.text = "Tag Manager";
        tagManagerTabBtn.style.flexGrow = 1;
        tagManagerTabBtn.AddToClassList("tab-button");
        tagManagerTabBtn.userData = TabType.TagManager;

        tabHeader.Add(taskBoardTabBtn);
        tabHeader.Add(tagManagerTabBtn);
        root.Add(tabHeader);

        // --- TAB CONTENT ---
        tabContentContainer = new VisualElement();
        tabContentContainer.style.flexGrow = 1;
        root.Add(tabContentContainer);

        // --- BOARD AREA ---
        boardScrollView = new ScrollView(ScrollViewMode.Horizontal);
        boardScrollView.AddToClassList("board-scroll-view");
        boardContainer = new VisualElement();
        boardContainer.AddToClassList("board-container");
        boardScrollView.Add(boardContainer);
        tabContentContainer.Add(boardScrollView);

        // --- TAG MANAGER TAB ---
        BuildTagManagerTab();
        tagContentContainer = new ScrollView();
        tagContentContainer.Add(tagManagerTab);
        tabContentContainer.Add(tagContentContainer);
        tagContentContainer.style.display = DisplayStyle.None;

        BuildDialogOverlay();
        BuildColumnNameDialogOverlay();
    }

    private void SwitchTab(TabType tab)
    {
        currentTab = tab;
        
        foreach (var child in tabHeader.Children())
        {
            if (child.userData is TabType childTab)
            {
                if (childTab == tab)
                {
                    child.AddToClassList("tab-active");
                }
                else
                {
                    child.RemoveFromClassList("tab-active");
                }
            }
        }

        if (tab == TabType.TaskBoard)
        {
            boardScrollView.style.display = DisplayStyle.Flex;
            tagContentContainer.style.display = DisplayStyle.None;
        }
        else
        {
            boardScrollView.style.display = DisplayStyle.None;
            tagContentContainer.style.display = DisplayStyle.Flex;
            RenderTagManagerTags();
        }
    }

    private void BuildDialogOverlay()
    {
        dialogOverlay = new VisualElement();
        dialogOverlay.AddToClassList("overlay");
        dialogOverlay.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
        dialogOverlay.style.display = DisplayStyle.None; // Ẩn mặc định

        var dialog = new VisualElement();
        dialog.AddToClassList("dialog");

        var dHeader = new VisualElement();
        dHeader.AddToClassList("dialog-header");
        var dTitle = new Label("Create Task");
        dTitle.AddToClassList("dialog-title-text");
        dHeader.Add(dTitle);
        dialog.Add(dHeader);

        var dBodyScroll = new ScrollView();
        dBodyScroll.AddToClassList("dialog-body-scroll");
        var dContent = new VisualElement();
        dContent.AddToClassList("dialog-content-container");

        var lblTitle = new Label("Title");
        lblTitle.AddToClassList("field-label");
        titleInput = new TextField();
        titleInput.AddToClassList("modern-input");
        dContent.Add(lblTitle);
        dContent.Add(titleInput);

        var lblDesc = new Label("Description");
        lblDesc.AddToClassList("field-label");
        lblDesc.style.marginTop = 10;
        descInput = new TextField();
        descInput.multiline = true;
        descInput.AddToClassList("modern-input");
        descInput.AddToClassList("input-multiline");
        dContent.Add(lblDesc);
        dContent.Add(descInput);

        var lblTag = new Label("Tags");
        lblTag.AddToClassList("field-label");
        lblTag.style.marginTop = 10;
        dContent.Add(lblTag);

        tagsContainer = new VisualElement();
        tagsContainer.style.flexDirection = FlexDirection.Row;
        tagsContainer.style.flexWrap = Wrap.Wrap;
        dContent.Add(tagsContainer);

        var tagSelectContainer = new VisualElement();
        tagSelectContainer.style.flexDirection = FlexDirection.Row;
        tagSelectContainer.style.marginBottom = 5;

        tagDropdown = new PopupField<string>(boardData.availableTags, boardData.availableTags.Count > 0 ? 0 : -1);
        tagDropdown.AddToClassList("modern-input");
        tagDropdown.style.flexGrow = 1;
        tagDropdown.style.marginRight = 5;

        var addTagBtn = new Button(AddSelectedTag);
        addTagBtn.text = "+ Add";
        addTagBtn.style.width = 60;
        addTagBtn.style.height = 28;
        addTagBtn.AddToClassList("secondary-button");

        tagSelectContainer.Add(tagDropdown);
        tagSelectContainer.Add(addTagBtn);

        var tagManagerHint = new Label("Manage tags via 'Manage Tags' button in header");
        tagManagerHint.style.fontSize = 11;
        tagManagerHint.style.color = new Color(0.5f, 0.5f, 0.6f);
        tagManagerHint.style.marginTop = 5;

        dContent.Add(tagSelectContainer);
        dContent.Add(tagManagerHint);

        var attachHeader = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 15 } };
        var lblAttach = new Label("Attachments");
        lblAttach.AddToClassList("field-label");
        
        var viewModeContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
        var btnGrid = CreateIconBtn("Grid", () => SwitchViewMode(true));
        var btnList = CreateIconBtn("List", () => SwitchViewMode(false));
        viewModeContainer.Add(btnGrid);
        viewModeContainer.Add(btnList);

        attachHeader.Add(lblAttach);
        attachHeader.Add(viewModeContainer);
        dContent.Add(attachHeader);

        dropArea = new VisualElement();
        dropArea.AddToClassList("drop-area");
        var dropLabel = new Label("Drag & Drop files here");
        dropLabel.AddToClassList("drop-area-label");
        dropArea.Add(dropLabel);

        var fileScroll = new ScrollView();
        fileScroll.AddToClassList("attached-files-scroll");
        attachedFilesContainer = new VisualElement();
        attachedFilesContainer.AddToClassList("attached-files-container");
        attachedFilesContainer.AddToClassList("grid-view");

        fileScroll.Add(attachedFilesContainer);
        dropArea.Add(fileScroll);

        // Drag Drop Logic cho Dialog
        dropArea.RegisterCallback<DragEnterEvent>(OnDragEnter);
        dropArea.RegisterCallback<DragLeaveEvent>(OnDragLeave);
        dropArea.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        dropArea.RegisterCallback<DragPerformEvent>(OnDragPerform);

        dContent.Add(dropArea);
        dBodyScroll.Add(dContent);
        dialog.Add(dBodyScroll);

        var dFooter = new VisualElement();
        dFooter.AddToClassList("dialog-footer");
        var btnCancel = new Button(CloseDialog) { text = "Cancel (Esc)" };
        btnCancel.AddToClassList("secondary-button");
        var btnSave = new Button(SaveTaskChanges) { text = "Save Changes (Enter)" };
        btnSave.AddToClassList("primary-button-large");
        dFooter.Add(btnCancel);
        dFooter.Add(btnSave);
        dialog.Add(dFooter);

        dialogOverlay.Add(dialog);
        root.Add(dialogOverlay);

        dialogOverlay.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Escape) CloseDialog();
            if (evt.keyCode == KeyCode.Return && evt.ctrlKey) SaveTaskChanges();
        });
    }

    private Button CreateIconBtn(string text, Action onClick)
    {
        var btn = new Button(onClick);
        btn.text = text == "Grid" ? "田" : "≡";
        btn.AddToClassList("icon-button");
        return btn;
    }

    private void RefreshBoard()
    {
        boardContainer.Clear();
        if (boardData == null) return;
        foreach (var col in boardData.columns)
        {
            boardContainer.Add(CreateColumnView(col));
        }
    }

    private VisualElement CreateColumnView(TaskColumn col)
    {
        var colEl = new VisualElement();
        colEl.AddToClassList("task-column");
        colEl.userData = col;

        var headerContainer = new VisualElement();
        headerContainer.AddToClassList("column-header-container");

        var titleContainer = new VisualElement();
        titleContainer.style.flexDirection = FlexDirection.Row;
        titleContainer.style.alignItems = Align.Center;
        titleContainer.style.flexGrow = 1;

        var titleLbl = new Label(col.title);
        titleLbl.AddToClassList("column-header");
        titleLbl.style.flexGrow = 1;

        var editBtn = new Button(() => RenameColumn(col, titleContainer, headerContainer));
        editBtn.text = "✎";
        editBtn.style.width = 20;
        editBtn.style.height = 20;
        editBtn.style.fontSize = 14;
        editBtn.style.backgroundColor = Color.clear;
        editBtn.style.borderTopWidth = 0;
        editBtn.style.borderBottomWidth = 0;
        editBtn.style.borderLeftWidth = 0;
        editBtn.style.borderRightWidth = 0;
        editBtn.style.color = new Color(0.7f, 0.7f, 0.8f);
        editBtn.style.paddingTop = 0;
        editBtn.style.paddingBottom = 0;
        editBtn.style.paddingLeft = 0;
        editBtn.style.paddingRight = 0;
        editBtn.style.marginLeft = 4;

        var countBadge = new Label(col.tasks.Count.ToString());
        countBadge.style.color = new Color(0.63f, 0.63f, 0.67f);
        countBadge.style.fontSize = 11;
        countBadge.style.backgroundColor = new Color(1f, 1f, 1f, 0.1f);
        countBadge.style.paddingTop = 2;
        countBadge.style.paddingBottom = 2;
        countBadge.style.paddingLeft = 6;
        countBadge.style.paddingRight = 6;
        countBadge.style.borderTopLeftRadius = 4;
        countBadge.style.borderTopRightRadius = 4;
        countBadge.style.borderBottomLeftRadius = 4;
        countBadge.style.borderBottomRightRadius = 4;
        countBadge.style.marginLeft = 8;

        titleContainer.Add(titleLbl);
        titleContainer.Add(countBadge);
        titleContainer.Add(editBtn);

        var delBtn = new Button(() =>
        {
            if (EditorUtility.DisplayDialog("Delete Column", $"Delete '{col.title}'?", "Yes", "No"))
            {
                boardData.columns.Remove(col);
                SaveData();
                RefreshBoard();
            }
        }) { text = "×" };
        delBtn.AddToClassList("column-delete-btn");

        headerContainer.Add(titleContainer);
        headerContainer.Add(delBtn);
        colEl.Add(headerContainer);

        var taskScroll = new ScrollView();
        taskScroll.AddToClassList("column-scroll");

        foreach (var task in col.tasks)
        {
            taskScroll.Add(CreateTaskCard(task, col));
        }
        colEl.Add(taskScroll);

        return colEl;
    }

    private void RenameColumn(TaskColumn col, VisualElement titleContainer, VisualElement headerContainer)
    {
        var tf = new TextField();
        tf.value = col.title;
        tf.style.flexGrow = 1;
        
        tf.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return)
            {
                col.title = tf.value;
                SaveData();
                RefreshBoard();
                evt.StopPropagation();
            }
        });

        tf.RegisterCallback<FocusOutEvent>(e =>
        {
            col.title = tf.value;
            SaveData();
            RefreshBoard();
        });
        
        headerContainer.Remove(titleContainer);
        headerContainer.Insert(0, tf);
        tf.Focus();
        tf.SelectAll();
    }

    // --- QUAN TRỌNG: Hàm tạo Card khớp với Manipulator ---
    private VisualElement CreateTaskCard(TaskData task, TaskColumn col)
    {
        var card = new VisualElement();
        card.AddToClassList("task-card");
        card.userData = task;

        card.AddManipulator(new TaskDragManipulator(this));

        card.RegisterCallback<ClickEvent>(evt => OpenEditTaskDialog(task, col));

        var title = new Label(task.title);
        title.AddToClassList("task-title");
        card.Add(title);

        if(!string.IsNullOrEmpty(task.description)) {
            var desc = new Label(task.description);
            desc.AddToClassList("task-description");
            card.Add(desc);
        }

        var footer = new VisualElement();
        footer.AddToClassList("card-footer");
        var date = new Label(task.date);
        date.AddToClassList("task-date");
        var delBtn = new Button(() =>
        {
            if (EditorUtility.DisplayDialog("Delete Task", "Are you sure?", "Yes", "No"))
            {
                col.tasks.Remove(task);
                SaveData();
                RefreshBoard();
            }
        }) { text = "×" };
        delBtn.AddToClassList("delete-button");
        delBtn.RegisterCallback<MouseDownEvent>(e => e.StopPropagation());

        footer.Add(date);
        footer.Add(delBtn);
        card.Add(footer);

        return card;
    }

    public void MoveTask(TaskData task, TaskColumn targetColumn)
    {
        foreach (var col in boardData.columns)
        {
            if (col.tasks.Contains(task))
            {
                if (col == targetColumn) return;
                col.tasks.Remove(task);
                break;
            }
        }
        targetColumn.tasks.Add(task);
        SaveData();
        RefreshBoard();
    }

    private void OpenCreateTaskDialog()
    {
        currentEditingTask = null;
        currentEditingColumn = null;
        titleInput.value = "";
        descInput.value = "";
        tempTags.Clear();
        tempAttachments.Clear();
        RenderTags();
        RenderAttachments();
        dialogOverlay.style.display = DisplayStyle.Flex;
    }

    private void OpenEditTaskDialog(TaskData task, TaskColumn col)
    {
        currentEditingTask = task;
        currentEditingColumn = col;
        titleInput.value = task.title;
        descInput.value = task.description;
        
        tempTags.Clear();
        if (task.checklist != null)
        {
            foreach (var item in task.checklist)
            {
                if (!string.IsNullOrEmpty(item.title))
                {
                    tempTags.Add(item.title);
                }
            }
        }
        RenderTags();
        
        if (task.attachments == null) task.attachments = new List<Object>();
        tempAttachments = new List<Object>(task.attachments);
        RenderAttachments();
        dialogOverlay.style.display = DisplayStyle.Flex;
    }

    private void CloseDialog()
    {
        dialogOverlay.style.display = DisplayStyle.None;
        currentEditingTask = null;
        currentEditingColumn = null;
        tempTags.Clear();
        tempAttachments.Clear();
    }

    private void SaveTaskChanges()
    {
        if (currentEditingTask == null)
        {
            var newTask = new TaskData
            {
                title = titleInput.value,
                description = descInput.value,
                date = DateTime.Now.ToString("MMM dd")
            };
            newTask.checklist = tempTags.Select(t => new ChecklistItem { title = t, isChecked = false }).ToList();
            newTask.attachments = new List<Object>(tempAttachments);
            
            var targetColumn = boardData.columns.Count > 0 ? boardData.columns[0] : null;
            if (targetColumn != null)
            {
                targetColumn.tasks.Add(newTask);
                SaveData();
                RefreshBoard();
            }
        }
        else
        {
            currentEditingTask.title = titleInput.value;
            currentEditingTask.description = descInput.value;
            currentEditingTask.checklist = tempTags.Select(t => new ChecklistItem { title = t, isChecked = false }).ToList();
            currentEditingTask.attachments = new List<Object>(tempAttachments);
            SaveData();
            RefreshBoard();
        }
        CloseDialog();
    }

    private void OnAddColumnClicked()
    {
        OpenColumnNameDialog();
    }

    private void SaveData()
    {
        if (boardData != null)
        {
            EditorUtility.SetDirty(boardData);
            AssetDatabase.SaveAssets();
        }
    }

    private void SwitchViewMode(bool grid)
    {
        isGridView = grid;
        if (isGridView) { attachedFilesContainer.RemoveFromClassList("list-view"); attachedFilesContainer.AddToClassList("grid-view"); }
        else { attachedFilesContainer.RemoveFromClassList("grid-view"); attachedFilesContainer.AddToClassList("list-view"); }
        RenderAttachments();
    }

    private void OnDragEnter(DragEnterEvent evt) { dropArea.AddToClassList("drop-area-hover"); }
    private void OnDragLeave(DragLeaveEvent evt) { dropArea.RemoveFromClassList("drop-area-hover"); }
    private void OnDragUpdated(DragUpdatedEvent evt) { DragAndDrop.visualMode = DragAndDropVisualMode.Copy; }
    private void OnDragPerform(DragPerformEvent evt)
    {
        dropArea.RemoveFromClassList("drop-area-hover");
        DragAndDrop.AcceptDrag();
        foreach (var obj in DragAndDrop.objectReferences)
            if (!tempAttachments.Contains(obj)) tempAttachments.Add(obj);
        RenderAttachments();
    }

    private void RenderAttachments()
    {
        attachedFilesContainer.Clear();
        tempAttachments.RemoveAll(x => x == null);
        foreach (var file in tempAttachments)
        {
            var item = new VisualElement();
            item.AddToClassList("file-item");
            var icon = new VisualElement();
            icon.AddToClassList("file-icon-image");
            Texture iconTex = AssetPreview.GetMiniThumbnail(file);
            if (iconTex != null) icon.style.backgroundImage = (Texture2D)iconTex;

            var nameLbl = new Label(file.name);
            nameLbl.AddToClassList("file-name");
            nameLbl.tooltip = file.name;

            item.RegisterCallback<ContextClickEvent>(evt =>
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove"), false, () => { tempAttachments.Remove(file); RenderAttachments(); });
                menu.ShowAsContext();
            });
            item.RegisterCallback<MouseDownEvent>(evt => { if (evt.clickCount == 2) EditorGUIUtility.PingObject(file); });

            item.Add(icon);
            item.Add(nameLbl);
            attachedFilesContainer.Add(item);
        }
    }

    private void BuildColumnNameDialogOverlay()
    {
        columnNameDialogOverlay = new VisualElement();
        columnNameDialogOverlay.AddToClassList("overlay");
        columnNameDialogOverlay.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
        columnNameDialogOverlay.style.display = DisplayStyle.None;

        var dialog = new VisualElement();
        dialog.AddToClassList("dialog");
        dialog.style.width = 350;

        var dHeader = new VisualElement();
        dHeader.AddToClassList("dialog-header");
        var dTitle = new Label("Add New Column");
        dTitle.AddToClassList("dialog-title-text");
        dHeader.Add(dTitle);
        dialog.Add(dHeader);

        var dBody = new VisualElement();
        dBody.AddToClassList("dialog-content-container");
        dBody.style.paddingLeft = 20;
        dBody.style.paddingRight = 20;
        dBody.style.paddingTop = 20;
        dBody.style.paddingBottom = 20;

        var lblName = new Label("Column Name");
        lblName.AddToClassList("field-label");
        columnNameInput = new TextField();
        columnNameInput.AddToClassList("modern-input");
        dBody.Add(lblName);
        dBody.Add(columnNameInput);

        dialog.Add(dBody);

        var dFooter = new VisualElement();
        dFooter.AddToClassList("dialog-footer");
        var btnCancel = new Button(CloseColumnNameDialog) { text = "Cancel" };
        btnCancel.AddToClassList("secondary-button");
        var btnSave = new Button(SaveColumnName) { text = "Create" };
        btnSave.AddToClassList("primary-button");
        dFooter.Add(btnCancel);
        dFooter.Add(btnSave);
        dialog.Add(dFooter);

        columnNameDialogOverlay.Add(dialog);
        root.Add(columnNameDialogOverlay);

        columnNameDialogOverlay.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Escape) CloseColumnNameDialog();
            if (evt.keyCode == KeyCode.Return) SaveColumnName();
        });
    }

    private void OpenColumnNameDialog()
    {
        columnNameInput.value = "New Column";
        columnNameInput.SelectAll();
        columnNameInput.Focus();
        columnNameDialogOverlay.style.display = DisplayStyle.Flex;
    }

    private void CloseColumnNameDialog()
    {
        columnNameDialogOverlay.style.display = DisplayStyle.None;
    }

    private void SaveColumnName()
    {
        if (!string.IsNullOrEmpty(columnNameInput.value))
        {
            boardData.columns.Add(new TaskColumn { title = columnNameInput.value });
            SaveData();
            RefreshBoard();
        }
        CloseColumnNameDialog();
    }

    private void BuildTagManagerTab()
    {
        tagManagerTab = new VisualElement();
        tagManagerTab.style.flexDirection = FlexDirection.Column;
        tagManagerTab.style.flexGrow = 1;

        var headerSection = new VisualElement();
        headerSection.style.flexDirection = FlexDirection.Column;
        headerSection.style.paddingTop = 30;
        headerSection.style.paddingBottom = 20;
        headerSection.style.paddingLeft = 30;
        headerSection.style.paddingRight = 30;
        headerSection.style.borderBottomWidth = 1;
        headerSection.style.borderBottomColor = new Color(0.15f, 0.15f, 0.2f);

        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.justifyContent = Justify.SpaceBetween;
        headerRow.style.alignItems = Align.Center;

        var titleSection = new VisualElement();
        titleSection.style.flexDirection = FlexDirection.Column;

        var title = new Label("Tag Manager");
        title.style.fontSize = 24;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(1f, 1f, 1f);

        var subtitle = new Label("Create and manage tags to organize your tasks");
        subtitle.style.fontSize = 12;
        subtitle.style.color = new Color(0.6f, 0.6f, 0.7f);
        subtitle.style.marginTop = 4;

        titleSection.Add(title);
        titleSection.Add(subtitle);

        var tagCountBadge = new VisualElement();
        tagCountBadge.style.flexDirection = FlexDirection.Row;
        tagCountBadge.style.alignItems = Align.Center;
        tagCountBadge.style.backgroundColor = new Color(0.2f, 0.6f, 1f);
        tagCountBadge.style.paddingTop = 8;
        tagCountBadge.style.paddingBottom = 8;
        tagCountBadge.style.paddingLeft = 16;
        tagCountBadge.style.paddingRight = 16;
        tagCountBadge.style.borderTopLeftRadius = 8;
        tagCountBadge.style.borderTopRightRadius = 8;
        tagCountBadge.style.borderBottomLeftRadius = 8;
        tagCountBadge.style.borderBottomRightRadius = 8;

        var countLabel = new Label("0 Tags");
        countLabel.style.fontSize = 14;
        countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        countLabel.style.color = Color.white;

        tagCountBadge.Add(countLabel);

        headerRow.Add(titleSection);
        headerRow.Add(tagCountBadge);
        headerSection.Add(headerRow);
        tagManagerTab.Add(headerSection);

        var contentSection = new VisualElement();
        contentSection.style.flexDirection = FlexDirection.Column;
        contentSection.style.flexGrow = 1;
        contentSection.style.paddingTop = 30;
        contentSection.style.paddingLeft = 30;
        contentSection.style.paddingRight = 30;

        var tagsSection = new VisualElement();
        tagsSection.style.flexDirection = FlexDirection.Column;
        tagsSection.style.flexGrow = 1;

        var tagsHeader = new VisualElement();
        tagsHeader.style.flexDirection = FlexDirection.Row;
        tagsHeader.style.justifyContent = Justify.SpaceBetween;
        tagsHeader.style.alignItems = Align.Center;
        tagsHeader.style.marginBottom = 15;

        var lblTags = new Label("All Tags");
        lblTags.style.fontSize = 16;
        lblTags.style.unityFontStyleAndWeight = FontStyle.Bold;
        lblTags.style.color = new Color(0.9f, 0.9f, 1f);

        tagsHeader.Add(lblTags);
        tagsSection.Add(tagsHeader);

        var tagsScrollView = new ScrollView();
        tagsScrollView.style.flexGrow = 1;
        tagsScrollView.style.minHeight = 400;
        tagsScrollView.style.maxHeight = 500;

        tagManagerTagsContainer = new VisualElement();
        tagManagerTagsContainer.style.flexDirection = FlexDirection.Column;

        tagsScrollView.Add(tagManagerTagsContainer);
        tagsSection.Add(tagsScrollView);
        contentSection.Add(tagsSection);

        var addSection = new VisualElement();
        addSection.style.flexDirection = FlexDirection.Column;
        addSection.style.marginTop = 30;
        addSection.style.paddingTop = 25;
        addSection.style.paddingBottom = 25;
        addSection.style.paddingLeft = 25;
        addSection.style.paddingRight = 25;
        addSection.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        addSection.style.borderTopLeftRadius = 12;
        addSection.style.borderTopRightRadius = 12;
        addSection.style.borderBottomLeftRadius = 12;
        addSection.style.borderBottomRightRadius = 12;

        var addHeader = new Label("Add New Tag");
        addHeader.style.fontSize = 14;
        addHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        addHeader.style.color = new Color(0.9f, 0.9f, 1f);
        addHeader.style.marginBottom = 15;

        var addTagContainer = new VisualElement();
        addTagContainer.style.flexDirection = FlexDirection.Row;
        addTagContainer.style.alignItems = Align.Center;

        tagManagerInput = new TextField();
        tagManagerInput.AddToClassList("modern-input");
        tagManagerInput.style.flexGrow = 1;
        tagManagerInput.style.marginRight = 15;
        tagManagerInput.style.height = 44;
        tagManagerInput.style.fontSize = 14;
        tagManagerInput.style.paddingTop = 0;
        tagManagerInput.style.paddingBottom = 0;
        tagManagerInput.style.paddingLeft = 15;
        tagManagerInput.style.paddingRight = 15;
        tagManagerInput.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        tagManagerInput.style.borderTopLeftRadius = 8;
        tagManagerInput.style.borderTopRightRadius = 8;
        tagManagerInput.style.borderBottomLeftRadius = 8;
        tagManagerInput.style.borderBottomRightRadius = 8;
        tagManagerInput.style.borderTopWidth = 1;
        tagManagerInput.style.borderBottomWidth = 1;
        tagManagerInput.style.borderLeftWidth = 1;
        tagManagerInput.style.borderRightWidth = 1;
        tagManagerInput.style.borderTopColor = new Color(0.3f, 0.3f, 0.4f);
        tagManagerInput.style.borderBottomColor = new Color(0.3f, 0.3f, 0.4f);
        tagManagerInput.style.borderLeftColor = new Color(0.3f, 0.3f, 0.4f);
        tagManagerInput.style.borderRightColor = new Color(0.3f, 0.3f, 0.4f);
        tagManagerInput.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return)
            {
                evt.StopPropagation();
                CreateNewTag();
            }
        });

        var addBtn = new Button(CreateNewTag);
        addBtn.text = "Create Tag";
        addBtn.style.width = 120;
        addBtn.style.height = 44;
        addBtn.style.fontSize = 14;
        addBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        addBtn.style.backgroundColor = new Color(0.2f, 0.6f, 1f);
        addBtn.style.borderTopLeftRadius = 8;
        addBtn.style.borderTopRightRadius = 8;
        addBtn.style.borderBottomLeftRadius = 8;
        addBtn.style.borderBottomRightRadius = 8;
        addBtn.style.borderTopWidth = 0;
        addBtn.style.borderBottomWidth = 0;
        addBtn.style.borderLeftWidth = 0;
        addBtn.style.borderRightWidth = 0;

        addTagContainer.Add(tagManagerInput);
        addTagContainer.Add(addBtn);

        addSection.Add(addHeader);
        addSection.Add(addTagContainer);
        contentSection.Add(addSection);

        tagManagerTab.Add(contentSection);
    }

    private void CreateNewTag()
    {
        string newTag = tagManagerInput.value.Trim();
        if (!string.IsNullOrEmpty(newTag) && !boardData.availableTags.Contains(newTag))
        {
            boardData.availableTags.Add(newTag);
            SaveData();
            tagManagerInput.value = "";
            RenderTagManagerTags();
            UpdateTagDropdown();
        }
    }

    private void DeleteTag(string tag)
    {
        boardData.availableTags.Remove(tag);
        SaveData();
        RenderTagManagerTags();
        UpdateTagDropdown();
    }

    private void RenderTagManagerTags()
    {
        tagManagerTagsContainer.Clear();

        if (boardData.availableTags.Count == 0)
        {
            var emptyState = new VisualElement();
            emptyState.style.flexDirection = FlexDirection.Column;
            emptyState.style.alignItems = Align.Center;
            emptyState.style.justifyContent = Justify.Center;
            emptyState.style.flexGrow = 1;
            emptyState.style.paddingTop = 60;
            emptyState.style.paddingBottom = 60;

            var emptyIcon = new Label("🏷");
            emptyIcon.style.fontSize = 48;
            emptyIcon.style.marginBottom = 15;

            var emptyTitle = new Label("No tags created yet");
            emptyTitle.style.fontSize = 16;
            emptyTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            emptyTitle.style.color = new Color(0.7f, 0.7f, 0.8f);
            emptyTitle.style.marginBottom = 8;

            var emptyDesc = new Label("Create your first tag to start organizing your tasks");
            emptyDesc.style.fontSize = 13;
            emptyDesc.style.color = new Color(0.5f, 0.5f, 0.6f);

            emptyState.Add(emptyIcon);
            emptyState.Add(emptyTitle);
            emptyState.Add(emptyDesc);
            tagManagerTagsContainer.Add(emptyState);

            UpdateTagCount(0);
            return;
        }

        UpdateTagCount(boardData.availableTags.Count);

        foreach (var tag in boardData.availableTags)
        {
            var tagColor = GetTagColor(tag);
            var taskCount = GetTaskCountForTag(tag);

            var tagRow = new VisualElement();
            tagRow.style.flexDirection = FlexDirection.Row;
            tagRow.style.alignItems = Align.Center;
            tagRow.style.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            tagRow.style.borderTopLeftRadius = 8;
            tagRow.style.borderTopRightRadius = 8;
            tagRow.style.borderBottomLeftRadius = 8;
            tagRow.style.borderBottomRightRadius = 8;
            tagRow.style.paddingTop = 12;
            tagRow.style.paddingBottom = 12;
            tagRow.style.paddingLeft = 16;
            tagRow.style.paddingRight = 16;
            tagRow.style.marginTop = 8;
            tagRow.style.marginBottom = 8;
            tagRow.style.borderTopWidth = 1;
            tagRow.style.borderBottomWidth = 1;
            tagRow.style.borderLeftWidth = 1;
            tagRow.style.borderRightWidth = 1;
            tagRow.style.borderTopColor = new Color(0.25f, 0.25f, 0.35f);
            tagRow.style.borderBottomColor = new Color(0.25f, 0.25f, 0.35f);
            tagRow.style.borderLeftColor = new Color(0.25f, 0.25f, 0.35f);
            tagRow.style.borderRightColor = new Color(0.25f, 0.25f, 0.35f);

            var colorIndicator = new VisualElement();
            colorIndicator.style.width = 6;
            colorIndicator.style.height = 6;
            colorIndicator.style.borderTopLeftRadius = 3;
            colorIndicator.style.borderTopRightRadius = 3;
            colorIndicator.style.borderBottomLeftRadius = 3;
            colorIndicator.style.borderBottomRightRadius = 3;
            colorIndicator.style.backgroundColor = tagColor;
            colorIndicator.style.marginRight = 14;

            var tagInfo = new VisualElement();
            tagInfo.style.flexDirection = FlexDirection.Column;
            tagInfo.style.flexGrow = 1;

            var tagName = new Label(tag);
            tagName.style.fontSize = 14;
            tagName.style.unityFontStyleAndWeight = FontStyle.Bold;
            tagName.style.color = new Color(0.95f, 0.95f, 1f);

            var tagStats = new Label($"{taskCount} task{(taskCount != 1 ? "s" : "")} using this tag");
            tagStats.style.fontSize = 12;
            tagStats.style.color = new Color(0.5f, 0.5f, 0.6f);
            tagStats.style.marginTop = 2;

            tagInfo.Add(tagName);
            tagInfo.Add(tagStats);

            var deleteBtn = new Button(() => DeleteTag(tag));
            deleteBtn.text = "×";
            deleteBtn.style.width = 32;
            deleteBtn.style.height = 32;
            deleteBtn.style.fontSize = 20;
            deleteBtn.style.color = new Color(0.8f, 0.5f, 0.5f);
            deleteBtn.style.backgroundColor = new Color(0.8f, 0.5f, 0.5f, 0.15f);
            deleteBtn.style.borderTopLeftRadius = 6;
            deleteBtn.style.borderTopRightRadius = 6;
            deleteBtn.style.borderBottomLeftRadius = 6;
            deleteBtn.style.borderBottomRightRadius = 6;
            deleteBtn.style.paddingTop = 0;
            deleteBtn.style.paddingBottom = 0;
            deleteBtn.style.paddingLeft = 0;
            deleteBtn.style.paddingRight = 0;
            deleteBtn.style.borderTopWidth = 0;
            deleteBtn.style.borderBottomWidth = 0;
            deleteBtn.style.borderLeftWidth = 0;
            deleteBtn.style.borderRightWidth = 0;

            tagRow.Add(colorIndicator);
            tagRow.Add(tagInfo);
            tagRow.Add(deleteBtn);
            tagManagerTagsContainer.Add(tagRow);
        }
    }

    private int GetTaskCountForTag(string tag)
    {
        int count = 0;
        foreach (var col in boardData.columns)
        {
            foreach (var task in col.tasks)
            {
                if (task.checklist != null)
                {
                    foreach (var item in task.checklist)
                    {
                        if (item.title == tag)
                        {
                            count++;
                            break;
                        }
                    }
                }
            }
        }
        return count;
    }

    private void UpdateTagCount(int count)
    {
        var headerSection = tagManagerTab[0];
        var headerRow = headerSection[0];
        var tagCountBadge = headerRow[1];
        var countLabel = tagCountBadge[0] as Label;
        countLabel.text = count == 1 ? "1 Tag" : $"{count} Tags";
    }

    private Color GetTagColor(string tag)
    {
        var hash = 0;
        foreach (var c in tag)
        {
            hash = hash * 31 + c;
        }

        var colors = new[]
        {
            new Color(0.3f, 0.5f, 0.9f),
            new Color(0.2f, 0.7f, 0.5f),
            new Color(0.9f, 0.5f, 0.2f),
            new Color(0.8f, 0.3f, 0.7f),
            new Color(0.3f, 0.8f, 0.7f),
            new Color(0.7f, 0.7f, 0.3f),
            new Color(0.9f, 0.4f, 0.5f)
        };

        var index = Math.Abs(hash) % colors.Length;
        return colors[index];
    }

    private void UpdateTagDropdown()
    {
        if (tagDropdown != null)
        {
            tagDropdown.choices = new List<string>(boardData.availableTags);
            if (boardData.availableTags.Count > 0)
            {
                tagDropdown.index = 0;
            }
        }
    }

    private void AddSelectedTag()
    {
        string tag = tagDropdown.value;
        if (!string.IsNullOrEmpty(tag) && !tempTags.Contains(tag))
        {
            tempTags.Add(tag);
            RenderTags();
        }
    }

    private void RemoveTag(string tag)
    {
        tempTags.Remove(tag);
        RenderTags();
    }

    private void RenderTags()
    {
        tagsContainer.Clear();
        
        if (tempTags.Count == 0)
        {
            var noTagsLabel = new Label("No tags selected - go to Tag Manager to add tags");
            noTagsLabel.style.fontSize = 11;
            noTagsLabel.style.color = new Color(0.5f, 0.5f, 0.6f);
            noTagsLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            tagsContainer.Add(noTagsLabel);
        }
        
        foreach (var tag in tempTags)
        {
            var tagEl = new VisualElement();
            tagEl.style.flexDirection = FlexDirection.Row;
            tagEl.style.alignItems = Align.Center;
            tagEl.style.backgroundColor = new Color(0.3f, 0.3f, 0.4f);
            tagEl.style.borderTopLeftRadius = 4;
            tagEl.style.borderTopRightRadius = 4;
            tagEl.style.borderBottomLeftRadius = 4;
            tagEl.style.borderBottomRightRadius = 4;
            tagEl.style.paddingTop = 4;
            tagEl.style.paddingBottom = 4;
            tagEl.style.paddingLeft = 8;
            tagEl.style.paddingRight = 8;
            tagEl.style.marginRight = 6;
            tagEl.style.marginTop = 4;
            tagEl.style.marginBottom = 4;

            var tagLbl = new Label(tag);
            tagLbl.style.fontSize = 11;
            tagLbl.style.color = new Color(0.8f, 0.8f, 0.9f);

            var removeBtn = new Button(() => RemoveTag(tag));
            removeBtn.text = "×";
            removeBtn.style.width = 16;
            removeBtn.style.height = 16;
            removeBtn.style.fontSize = 12;
            removeBtn.style.color = new Color(1f, 0.6f, 0.6f);
            removeBtn.style.marginLeft = 6;
            removeBtn.style.paddingTop = 0;
            removeBtn.style.paddingBottom = 0;
            removeBtn.style.paddingLeft = 0;
            removeBtn.style.paddingRight = 0;

            tagEl.Add(tagLbl);
            tagEl.Add(removeBtn);
            tagsContainer.Add(tagEl);
        }
    }
}
}

