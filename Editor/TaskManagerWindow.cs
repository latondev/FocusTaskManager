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
        private enum TabType { TaskBoard, Dashboard, SprintTimeline, TagManager }
        private TabType currentTab = TabType.TaskBoard;

        private VisualElement dashboardTab;
        private ScrollView dashboardContentContainer;

        private VisualElement sprintTimelineTab;
        private ScrollView sprintTimelineContentContainer;
        private PopupField<string> sprintSelectorDropdown;
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

            // Tải file CSS đúng đường dẫn
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Module/FocusTaskManager/GUI/TaskBoard.uss");
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            BuildMainLayout();
            RefreshBoard();
        }

        private void LoadData()
        {
            // Đảm bảo thư mục tồn tại
            if (!System.IO.Directory.Exists("Assets/Module/FocusTaskManager/GUI"))
                System.IO.Directory.CreateDirectory("Assets/Module/FocusTaskManager/GUI");

            string path = "Assets/Module/FocusTaskManager/GUI/TaskBoardData.asset";
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

            var dashboardTabBtn = new Button(() => SwitchTab(TabType.Dashboard));
            dashboardTabBtn.text = "Dashboard";
            dashboardTabBtn.style.flexGrow = 1;
            dashboardTabBtn.AddToClassList("tab-button");
            dashboardTabBtn.userData = TabType.Dashboard;

            var sprintTimelineTabBtn = new Button(() => SwitchTab(TabType.SprintTimeline));
            sprintTimelineTabBtn.text = "Sprint Timeline";
            sprintTimelineTabBtn.style.flexGrow = 1;
            sprintTimelineTabBtn.AddToClassList("tab-button");
            sprintTimelineTabBtn.userData = TabType.SprintTimeline;

            var tagManagerTabBtn = new Button(() => SwitchTab(TabType.TagManager));
            tagManagerTabBtn.text = "Tag Manager";
            tagManagerTabBtn.style.flexGrow = 1;
            tagManagerTabBtn.AddToClassList("tab-button");
            tagManagerTabBtn.userData = TabType.TagManager;

            tabHeader.Add(taskBoardTabBtn);
            tabHeader.Add(dashboardTabBtn);
            tabHeader.Add(sprintTimelineTabBtn);
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

            // --- DASHBOARD TAB ---
            BuildDashboardTab();
            dashboardContentContainer = new ScrollView();
            dashboardContentContainer.Add(dashboardTab);
            tabContentContainer.Add(dashboardContentContainer);
            dashboardContentContainer.style.display = DisplayStyle.None;

            // --- SPRINT TIMELINE TAB ---
            BuildSprintTimelineTab();
            sprintTimelineContentContainer = new ScrollView();
            sprintTimelineContentContainer.Add(sprintTimelineTab);
            tabContentContainer.Add(sprintTimelineContentContainer);
            sprintTimelineContentContainer.style.display = DisplayStyle.None;

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
                dashboardContentContainer.style.display = DisplayStyle.None;
                sprintTimelineContentContainer.style.display = DisplayStyle.None;
                tagContentContainer.style.display = DisplayStyle.None;
                RefreshBoard();
            }
            else if (tab == TabType.Dashboard)
            {
                boardScrollView.style.display = DisplayStyle.None;
                dashboardContentContainer.style.display = DisplayStyle.Flex;
                sprintTimelineContentContainer.style.display = DisplayStyle.None;
                tagContentContainer.style.display = DisplayStyle.None;
                RenderDashboard();
            }
            else if (tab == TabType.SprintTimeline)
            {
                boardScrollView.style.display = DisplayStyle.None;
                dashboardContentContainer.style.display = DisplayStyle.None;
                sprintTimelineContentContainer.style.display = DisplayStyle.Flex;
                tagContentContainer.style.display = DisplayStyle.None;
                RenderSprintTimeline();
            }
            else
            {
                boardScrollView.style.display = DisplayStyle.None;
                dashboardContentContainer.style.display = DisplayStyle.None;
                sprintTimelineContentContainer.style.display = DisplayStyle.None;
                tagContentContainer.style.display = DisplayStyle.Flex;
                RenderTagManagerTags();
            }
        }

        private PopupField<TaskPriority> priorityDropdown;
        private PopupField<string> assigneeDropdown;
        private PopupField<string> sprintDropdown;
        private TextField startDateInput;
        private TextField dueDateInput;
        private TaskPriority tempPriority;
        private List<string> tempAssignees = new List<string>();
        private VisualElement assigneesContainer;

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

            // Row for Priority & Assignees
            var rowPriorityAssignee = new VisualElement();
            rowPriorityAssignee.style.flexDirection = FlexDirection.Row;
            rowPriorityAssignee.style.justifyContent = Justify.SpaceBetween;
            rowPriorityAssignee.style.marginTop = 10;

            // Priority
            var prioContainer = new VisualElement();
            prioContainer.style.flexGrow = 1;
            prioContainer.style.marginRight = 10;
            var lblPrio = new Label("Priority");
            lblPrio.AddToClassList("field-label");
            priorityDropdown = new PopupField<TaskPriority>(Enum.GetValues(typeof(TaskPriority)).Cast<TaskPriority>().ToList(), 0);
            priorityDropdown.AddToClassList("modern-input");
            prioContainer.Add(lblPrio);
            prioContainer.Add(priorityDropdown);
            rowPriorityAssignee.Add(prioContainer);

            // Assignees
            var assigneeContainer = new VisualElement();
            assigneeContainer.style.flexGrow = 1;
            var lblAssignee = new Label("Assignees");
            lblAssignee.AddToClassList("field-label");

            var assignSelectRow = new VisualElement();
            assignSelectRow.style.flexDirection = FlexDirection.Row;
            var assigneeChoices = boardData.availableAssignees != null ? boardData.availableAssignees.Select(a => a.name).ToList() : new List<string>();
            if (assigneeChoices.Count == 0) assigneeChoices.Add("None");
            assigneeDropdown = new PopupField<string>(assigneeChoices, 0);
            assigneeDropdown.AddToClassList("modern-input");
            assigneeDropdown.style.flexGrow = 1;
            assigneeDropdown.style.marginRight = 5;

            var addAssignBtn = new Button(AddSelectedAssignee);
            addAssignBtn.text = "+";
            addAssignBtn.AddToClassList("secondary-button");
            assignSelectRow.Add(assigneeDropdown);
            assignSelectRow.Add(addAssignBtn);

            assigneesContainer = new VisualElement();
            assigneesContainer.style.flexDirection = FlexDirection.Row;
            assigneesContainer.style.flexWrap = Wrap.Wrap;
            assigneesContainer.style.marginTop = 5;

            assigneeContainer.Add(lblAssignee);
            assigneeContainer.Add(assignSelectRow);
            assigneeContainer.Add(assigneesContainer);
            rowPriorityAssignee.Add(assigneeContainer);

            dContent.Add(rowPriorityAssignee);

            // Row for Sprint & Dates
            var rowSprintDates = new VisualElement();
            rowSprintDates.style.flexDirection = FlexDirection.Row;
            rowSprintDates.style.justifyContent = Justify.SpaceBetween;
            rowSprintDates.style.marginTop = 10;

            var sprintContainer = new VisualElement();
            sprintContainer.style.flexGrow = 1;
            sprintContainer.style.marginRight = 10;
            var lblSprint = new Label("Sprint");
            lblSprint.AddToClassList("field-label");

            var sprintChoices = boardData.sprints != null ? boardData.sprints.Select(s => s.title).ToList() : new List<string>();
            sprintChoices.Insert(0, "None");
            sprintDropdown = new PopupField<string>(sprintChoices, 0);
            sprintDropdown.AddToClassList("modern-input");
            sprintContainer.Add(lblSprint);
            sprintContainer.Add(sprintDropdown);
            rowSprintDates.Add(sprintContainer);

            var datesContainer = new VisualElement();
            datesContainer.style.flexGrow = 1;
            datesContainer.style.flexDirection = FlexDirection.Row;

            var startDateContainer = new VisualElement();
            startDateContainer.style.flexGrow = 1;
            startDateContainer.style.marginRight = 5;
            var lblStart = new Label("Start (MM-dd)");
            lblStart.style.fontSize = 11;
            lblStart.style.color = new Color(0.6f, 0.6f, 0.7f);
            startDateInput = new TextField();
            startDateInput.AddToClassList("modern-input");
            startDateContainer.Add(lblStart);
            startDateContainer.Add(startDateInput);

            var dueDateContainer = new VisualElement();
            dueDateContainer.style.flexGrow = 1;
            var lblDue = new Label("Due (MM-dd)");
            lblDue.style.fontSize = 11;
            lblDue.style.color = new Color(0.6f, 0.6f, 0.7f);
            dueDateInput = new TextField();
            dueDateInput.AddToClassList("modern-input");
            dueDateContainer.Add(lblDue);
            dueDateContainer.Add(dueDateInput);

            datesContainer.Add(startDateContainer);
            datesContainer.Add(dueDateContainer);
            rowSprintDates.Add(datesContainer);

            dContent.Add(rowSprintDates);

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

            var availableTags = boardData.availableTags != null ? boardData.availableTags : new List<string>();
            tagDropdown = new PopupField<string>(availableTags, availableTags.Count > 0 ? 0 : -1);
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

        private void AddSelectedAssignee()
        {
            string assignee = assigneeDropdown.value;
            if (!string.IsNullOrEmpty(assignee) && assignee != "None" && !tempAssignees.Contains(assignee))
            {
                tempAssignees.Add(assignee);
                RenderAssignees();
            }
        }

        private void RemoveAssignee(string assignee)
        {
            tempAssignees.Remove(assignee);
            RenderAssignees();
        }

        private void RenderAssignees()
        {
            if (assigneesContainer == null) return;
            assigneesContainer.Clear();

            foreach (var assignee in tempAssignees)
            {
                var el = new VisualElement();
                el.style.flexDirection = FlexDirection.Row;
                el.style.alignItems = Align.Center;
                el.style.backgroundColor = GetAssigneeColor(assignee);
                el.style.borderTopLeftRadius = 4;
                el.style.borderTopRightRadius = 4;
                el.style.borderBottomLeftRadius = 4;
                el.style.borderBottomRightRadius = 4;
                el.style.paddingTop = 2;
                el.style.paddingBottom = 2;
                el.style.paddingLeft = 6;
                el.style.paddingRight = 6;
                el.style.marginRight = 4;
                el.style.marginTop = 4;

                var lbl = new Label(assignee);
                lbl.style.fontSize = 10;
                lbl.style.color = Color.white;
                lbl.style.unityFontStyleAndWeight = FontStyle.Bold;

                var btn = new Button(() => RemoveAssignee(assignee));
                btn.text = "×";
                btn.style.width = 14;
                btn.style.height = 14;
                btn.style.fontSize = 10;
                btn.style.color = new Color(1f, 0.8f, 0.8f);
                btn.style.backgroundColor = Color.clear;
                btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0; btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
                btn.style.paddingTop = 0; btn.style.paddingBottom = 0; btn.style.paddingLeft = 0; btn.style.paddingRight = 0;
                btn.style.marginLeft = 4;

                el.Add(lbl);
                el.Add(btn);
                assigneesContainer.Add(el);
            }
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
            })
            { text = "×" };
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

            // 1. Tags Container (Top of card)
            if (task.tags != null && task.tags.Count > 0)
            {
                var tagsContainer = new VisualElement();
                tagsContainer.AddToClassList("task-card-tags-container");
                foreach (var tag in task.tags)
                {
                    var tagLbl = new Label(tag);
                    tagLbl.AddToClassList("task-card-tag");
                    tagLbl.style.backgroundColor = GetTagColor(tag);
                    // Make text color dark or light depending on background (simple heuristic: mostly white for premium look)
                    tagLbl.style.color = Color.white;
                    tagsContainer.Add(tagLbl);
                }
                card.Add(tagsContainer);
            }

            // 2. Title
            var title = new Label(task.title);
            title.AddToClassList("task-title");
            card.Add(title);

            // 3. Description (Optional preview)
            if (!string.IsNullOrEmpty(task.description))
            {
                // Chỉ hiện 1 dòng preview mô tả
                string prefixDesc = task.description.Length > 50 ? task.description.Substring(0, 47) + "..." : task.description;
                var desc = new Label(prefixDesc);
                desc.AddToClassList("task-description");
                card.Add(desc);
            }

            // 4. Footer Layer 1 (Badges: Priority, Attachments, Checklists)
            var badgesContainer = new VisualElement();
            badgesContainer.AddToClassList("task-badges-container");
            badgesContainer.style.marginTop = 8;
            badgesContainer.style.flexWrap = Wrap.Wrap;

            // Attachment Badge
            if (task.attachments != null && task.attachments.Count > 0)
            {
                var attBadge = new VisualElement();
                attBadge.AddToClassList("attachment-badge");

                var icon = new Label("📎");
                icon.AddToClassList("attachment-badge-icon");
                var text = new Label(task.attachments.Count.ToString());
                text.AddToClassList("attachment-badge-text");

                attBadge.Add(icon);
                attBadge.Add(text);
                badgesContainer.Add(attBadge);
            }

            // Checklist Badge
            if (task.checklist != null && task.checklist.Count > 0)
            {
                var chkBadge = new VisualElement();
                chkBadge.AddToClassList("attachment-badge"); // Reuse style

                var icon = new Label("☑");
                icon.AddToClassList("attachment-badge-icon");
                var doneCount = task.checklist.Count(x => x.isChecked);
                var text = new Label($"{doneCount}/{task.checklist.Count}");
                text.AddToClassList("attachment-badge-text");
                if (doneCount == task.checklist.Count) text.style.color = new Color(0.4f, 0.9f, 0.4f); // Green if all done

                chkBadge.Add(icon);
                chkBadge.Add(text);
                badgesContainer.Add(chkBadge);
            }

            card.Add(badgesContainer);

            // 5. Footer Layer 2 (Date, Assignees, Priority, Delete)
            var footer = new VisualElement();
            footer.AddToClassList("card-footer");

            var date = new Label(string.IsNullOrEmpty(task.date) ? "No Date" : task.date);
            date.AddToClassList("task-date");

            // Info container right side
            var rightInfoContainer = new VisualElement();
            rightInfoContainer.style.flexDirection = FlexDirection.Row;
            rightInfoContainer.style.alignItems = Align.Center;

            // Assignees Avatars
            if (task.assignees != null && task.assignees.Count > 0)
            {
                var avatars = new VisualElement();
                avatars.AddToClassList("assignee-avatar-container");

                int maxAvatars = 3;
                for (int i = 0; i < Mathf.Min(task.assignees.Count, maxAvatars); i++)
                {
                    var assigneeName = task.assignees[i];
                    var avatar = new VisualElement();
                    avatar.AddToClassList("assignee-avatar");
                    avatar.style.backgroundColor = GetAssigneeColor(assigneeName);

                    var initial = new Label(assigneeName.Length > 0 ? assigneeName.Substring(0, 1).ToUpper() : "?");
                    initial.AddToClassList("assignee-avatar-text");
                    avatar.Add(initial);

                    // Add overlapping effect (zIndex isn't fully supported in old UI Toolkit easily, so we just append in order)
                    avatars.Add(avatar);
                }
                if (task.assignees.Count > maxAvatars)
                {
                    var extra = new VisualElement();
                    extra.AddToClassList("assignee-avatar");
                    extra.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                    var extraLbl = new Label($"+{task.assignees.Count - maxAvatars}");
                    extraLbl.AddToClassList("assignee-avatar-text");
                    extra.Add(extraLbl);
                    avatars.Add(extra);
                }
                rightInfoContainer.Add(avatars);
            }

            // Priority
            if (task.priority != TaskPriority.None)
            {
                var prio = new VisualElement();
                prio.AddToClassList("priority-badge");

                var iconBtn = new Button();
                iconBtn.text = "!";
                iconBtn.AddToClassList("icon-button");
                iconBtn.style.width = 20; iconBtn.style.height = 20;

                switch (task.priority)
                {
                    case TaskPriority.Low: iconBtn.style.color = new Color(0.3f, 0.8f, 0.3f); break; // Green
                    case TaskPriority.Medium: iconBtn.style.color = new Color(0.9f, 0.7f, 0.2f); break; // Yellow
                    case TaskPriority.High: iconBtn.style.color = new Color(0.9f, 0.3f, 0.3f); iconBtn.text = "!!"; break; // Red
                }
                rightInfoContainer.Add(iconBtn);
            }

            var delBtn = new Button(() =>
            {
                if (EditorUtility.DisplayDialog("Delete Task", "Are you sure?", "Yes", "No"))
                {
                    col.tasks.Remove(task);
                    SaveData();
                    RefreshBoard();
                }
            })
            { text = "×" };
            delBtn.AddToClassList("delete-button");
            delBtn.RegisterCallback<MouseDownEvent>(e => e.StopPropagation());

            rightInfoContainer.Add(delBtn);

            footer.Add(date);
            footer.Add(rightInfoContainer);
            card.Add(footer);

            return card;
        }

        private Color GetAssigneeColor(string assigneeName)
        {
            if (boardData != null && boardData.availableAssignees != null)
            {
                var found = boardData.availableAssignees.Find(a => a.name == assigneeName);
                if (found != null) return found.color;
            }
            return new Color(0.4f, 0.4f, 0.4f); // Default gray
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
            priorityDropdown.value = TaskPriority.None;
            sprintDropdown.value = "None";
            startDateInput.value = "";
            dueDateInput.value = "";
            tempTags.Clear();
            tempAssignees.Clear();
            tempAttachments.Clear();
            RenderTags();
            RenderAssignees();
            RenderAttachments();
            dialogOverlay.style.display = DisplayStyle.Flex;
        }

        private void OpenEditTaskDialog(TaskData task, TaskColumn col)
        {
            currentEditingTask = task;
            currentEditingColumn = col;
            titleInput.value = task.title;
            descInput.value = task.description;
            priorityDropdown.value = task.priority;

            string sTitle = "None";
            if (!string.IsNullOrEmpty(task.sprintId))
            {
                var sp = boardData.sprints.FirstOrDefault(s => s.id == task.sprintId);
                if (sp != null) sTitle = sp.title;
            }
            sprintDropdown.value = sTitle;
            startDateInput.value = task.startDate;
            dueDateInput.value = task.dueDate;

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

            tempAssignees.Clear();
            if (task.assignees != null) tempAssignees.AddRange(task.assignees);
            RenderAssignees();

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
            tempAssignees.Clear();
            tempAttachments.Clear();
        }

        private void SaveTaskChanges()
        {
            string spId = "";
            if (sprintDropdown.value != "None")
            {
                var sp = boardData.sprints.FirstOrDefault(s => s.title == sprintDropdown.value);
                if (sp != null) spId = sp.id;
            }

            if (currentEditingTask == null)
            {
                var newTask = new TaskData
                {
                    title = titleInput.value,
                    description = descInput.value,
                    date = DateTime.Now.ToString("MMM dd"),
                    priority = priorityDropdown.value,
                    assignees = new List<string>(tempAssignees),
                    sprintId = spId,
                    startDate = startDateInput.value,
                    dueDate = dueDateInput.value
                };
                newTask.checklist = tempTags.Select(t => new ChecklistItem { title = t, isChecked = false }).ToList();
                newTask.tags = new List<string>(tempTags); // Ensure Tags list is synced too
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
                currentEditingTask.priority = priorityDropdown.value;
                currentEditingTask.assignees = new List<string>(tempAssignees);
                currentEditingTask.sprintId = spId;
                currentEditingTask.startDate = startDateInput.value;
                currentEditingTask.dueDate = dueDateInput.value;
                currentEditingTask.checklist = tempTags.Select(t => new ChecklistItem { title = t, isChecked = false }).ToList();
                currentEditingTask.tags = new List<string>(tempTags); // Update Tags list
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

        private void BuildDashboardTab()
        {
            dashboardTab = new VisualElement();
            dashboardTab.AddToClassList("dashboard-tab");
            dashboardTab.style.paddingTop = 20;
            dashboardTab.style.paddingBottom = 20;
            dashboardTab.style.paddingLeft = 20;
            dashboardTab.style.paddingRight = 20;
            dashboardTab.style.flexDirection = FlexDirection.Column;
        }

        private void RenderDashboard()
        {
            dashboardTab.Clear();

            var title = new Label("Dashboard Overview");
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.95f, 0.95f, 1f);
            title.style.marginBottom = 20;
            dashboardTab.Add(title);

            // Statistics Overview
            var statsRow = new VisualElement();
            statsRow.style.flexDirection = FlexDirection.Row;
            statsRow.style.justifyContent = Justify.SpaceBetween;
            statsRow.style.marginBottom = 30;

            int totalTasks = 0;
            int doneTasks = 0;

            var lastCol = boardData.columns.Count > 0 ? boardData.columns[boardData.columns.Count - 1] : null;

            foreach (var col in boardData.columns)
            {
                totalTasks += col.tasks.Count;
                if (col == lastCol || col.title.ToLower().Contains("done"))
                {
                    doneTasks += col.tasks.Count;
                }
            }

            statsRow.Add(CreateStatCard("Total Tasks", totalTasks.ToString()));
            statsRow.Add(CreateStatCard("Completed", doneTasks.ToString()));
            statsRow.Add(CreateStatCard("Progress", totalTasks > 0 ? $"{(doneTasks * 100f / totalTasks):0}%" : "0%"));
            dashboardTab.Add(statsRow);

            var chartsRow = new VisualElement();
            chartsRow.style.flexDirection = FlexDirection.Row;

            var colDist = CreateColumnDistributionChart(totalTasks);
            colDist.style.flexGrow = 1;
            colDist.style.marginRight = 10;

            var tagDist = CreateTagDistributionChart(totalTasks);
            tagDist.style.flexGrow = 1;
            tagDist.style.marginLeft = 10;

            chartsRow.Add(colDist);
            chartsRow.Add(tagDist);
            dashboardTab.Add(chartsRow);
        }

        private VisualElement CreateStatCard(string label, string value)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            card.style.borderTopLeftRadius = 8;
            card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = 8;
            card.style.borderBottomRightRadius = 8;
            card.style.paddingTop = 15;
            card.style.paddingBottom = 15;
            card.style.paddingLeft = 20;
            card.style.paddingRight = 20;
            card.style.flexGrow = 1;
            card.style.marginLeft = 5;
            card.style.marginRight = 5;

            var valLbl = new Label(value);
            valLbl.style.fontSize = 28;
            valLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            valLbl.style.color = new Color(0.4f, 0.7f, 1f);

            var titleLbl = new Label(label);
            titleLbl.style.fontSize = 12;
            titleLbl.style.color = new Color(0.6f, 0.6f, 0.7f);
            titleLbl.style.marginTop = 5;

            card.Add(valLbl);
            card.Add(titleLbl);
            return card;
        }

        private VisualElement CreateColumnDistributionChart(int totalTasks)
        {
            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            container.style.paddingTop = 15; container.style.paddingBottom = 15;
            container.style.paddingLeft = 15; container.style.paddingRight = 15;
            container.style.borderTopLeftRadius = 8; container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8; container.style.borderBottomRightRadius = 8;

            var title = new Label("Tasks by Column");
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 15;
            container.Add(title);

            if (totalTasks == 0)
            {
                var empty = new Label("No tasks found");
                empty.style.color = new Color(0.5f, 0.5f, 0.6f);
                container.Add(empty);
                return container;
            }

            foreach (var col in boardData.columns)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 10;

                var cTitle = new Label(col.title);
                cTitle.style.width = 120;
                cTitle.style.color = new Color(0.8f, 0.8f, 0.9f);
                cTitle.style.fontSize = 12;

                var barContainer = new VisualElement();
                barContainer.style.flexGrow = 1;
                barContainer.style.height = 10;
                barContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
                barContainer.style.borderTopLeftRadius = 5; barContainer.style.borderTopRightRadius = 5;
                barContainer.style.borderBottomLeftRadius = 5; barContainer.style.borderBottomRightRadius = 5;
                barContainer.style.marginRight = 10;

                var fill = new VisualElement();
                float pct = (float)col.tasks.Count / totalTasks;
                fill.style.width = new Length(pct * 100, LengthUnit.Percent);
                fill.style.height = 10;
                fill.style.backgroundColor = new Color(0.4f, 0.8f, 0.6f);
                fill.style.borderTopLeftRadius = 5; fill.style.borderTopRightRadius = 5;
                fill.style.borderBottomLeftRadius = 5; fill.style.borderBottomRightRadius = 5;
                barContainer.Add(fill);

                var countLbl = new Label(col.tasks.Count.ToString());
                countLbl.style.width = 30;
                countLbl.style.unityTextAlign = TextAnchor.MiddleRight;
                countLbl.style.color = Color.white;

                row.Add(cTitle);
                row.Add(barContainer);
                row.Add(countLbl);
                container.Add(row);
            }

            return container;
        }

        private VisualElement CreateTagDistributionChart(int totalTasks)
        {
            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            container.style.paddingTop = 15; container.style.paddingBottom = 15;
            container.style.paddingLeft = 15; container.style.paddingRight = 15;
            container.style.borderTopLeftRadius = 8; container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8; container.style.borderBottomRightRadius = 8;

            var title = new Label("Tasks by Tag");
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 15;
            container.Add(title);

            if (boardData.availableTags == null || boardData.availableTags.Count == 0)
            {
                var empty = new Label("No tags found");
                empty.style.color = new Color(0.5f, 0.5f, 0.6f);
                container.Add(empty);
                return container;
            }

            bool hasAnyTag = false;
            foreach (var tag in boardData.availableTags)
            {
                int count = GetTaskCountForTag(tag);
                if (count > 0)
                {
                    hasAnyTag = true;
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.marginBottom = 10;

                    var tTitle = new Label(tag);
                    tTitle.style.width = 80;
                    tTitle.style.color = new Color(0.8f, 0.8f, 0.9f);
                    tTitle.style.fontSize = 12;

                    var barContainer = new VisualElement();
                    barContainer.style.flexGrow = 1;
                    barContainer.style.height = 10;
                    barContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
                    barContainer.style.borderTopLeftRadius = 5; barContainer.style.borderTopRightRadius = 5;
                    barContainer.style.borderBottomLeftRadius = 5; barContainer.style.borderBottomRightRadius = 5;
                    barContainer.style.marginRight = 10;

                    var fill = new VisualElement();
                    float pct = (float)count / totalTasks;
                    fill.style.width = new Length(pct * 100, LengthUnit.Percent);
                    fill.style.height = 10;
                    fill.style.backgroundColor = GetTagColor(tag);
                    fill.style.borderTopLeftRadius = 5; fill.style.borderTopRightRadius = 5;
                    fill.style.borderBottomLeftRadius = 5; fill.style.borderBottomRightRadius = 5;
                    barContainer.Add(fill);

                    var countLbl = new Label(count.ToString());
                    countLbl.style.width = 30;
                    countLbl.style.unityTextAlign = TextAnchor.MiddleRight;
                    countLbl.style.color = Color.white;

                    row.Add(tTitle);
                    row.Add(barContainer);
                    row.Add(countLbl);
                    container.Add(row);
                }
            }

            if (!hasAnyTag)
            {
                var empty = new Label("No tags assigned to tasks");
                empty.style.color = new Color(0.5f, 0.5f, 0.6f);
                container.Add(empty);
            }

            return container;
        }

        private void BuildSprintTimelineTab()
        {
            sprintTimelineTab = new VisualElement();
            sprintTimelineTab.AddToClassList("dashboard-tab");
            sprintTimelineTab.style.paddingTop = 20;
            sprintTimelineTab.style.paddingBottom = 20;
            sprintTimelineTab.style.paddingLeft = 20;
            sprintTimelineTab.style.paddingRight = 20;
            sprintTimelineTab.style.flexDirection = FlexDirection.Column;
        }

        private void RenderSprintTimeline()
        {
            sprintTimelineTab.Clear();

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 20;

            var title = new Label("Sprint Timeline");
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.95f, 0.95f, 1f);

            var controlsRow = new VisualElement();
            controlsRow.style.flexDirection = FlexDirection.Row;
            controlsRow.style.alignItems = Align.Center;

            var lblSelect = new Label("Select Sprint:");
            lblSelect.style.color = new Color(0.7f, 0.7f, 0.8f);
            lblSelect.style.marginRight = 10;

            var sprintChoices = boardData.sprints != null ? boardData.sprints.Select(s => s.title).ToList() : new List<string>();
            if (sprintChoices.Count == 0) sprintChoices.Add("None");

            sprintSelectorDropdown = new PopupField<string>(sprintChoices, 0);
            sprintSelectorDropdown.AddToClassList("modern-input");
            sprintSelectorDropdown.RegisterValueChangedCallback(evt => RefreshSprintTimelineView(evt.newValue));

            var addSprintBtn = new Button(CreateNewSprint);
            addSprintBtn.text = "+ New Sprint";
            addSprintBtn.AddToClassList("secondary-button");
            addSprintBtn.style.marginLeft = 10;

            controlsRow.Add(lblSelect);
            controlsRow.Add(sprintSelectorDropdown);
            controlsRow.Add(addSprintBtn);

            headerRow.Add(title);
            headerRow.Add(controlsRow);
            sprintTimelineTab.Add(headerRow);

            var timelineContainer = new ScrollView();
            timelineContainer.style.flexGrow = 1;
            timelineContainer.style.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            timelineContainer.style.borderTopLeftRadius = 8; timelineContainer.style.borderTopRightRadius = 8;
            timelineContainer.style.borderBottomLeftRadius = 8; timelineContainer.style.borderBottomRightRadius = 8;
            timelineContainer.style.paddingTop = 15; timelineContainer.style.paddingBottom = 15;
            timelineContainer.style.paddingLeft = 15; timelineContainer.style.paddingRight = 15;

            // Render tasks for active sprint
            string selectedSprintTitle = sprintSelectorDropdown.value;
            var sp = boardData.sprints != null ? boardData.sprints.FirstOrDefault(s => s.title == selectedSprintTitle) : null;

            if (sp == null)
            {
                var empty = new Label("Choose a valid sprint or create a new one to see tasks.");
                empty.style.color = new Color(0.5f, 0.5f, 0.6f);
                timelineContainer.Add(empty);
            }
            else
            {
                var sprintInfo = new Label($"{sp.title} | {sp.startDate} - {sp.endDate}");
                sprintInfo.style.fontSize = 14;
                sprintInfo.style.color = new Color(0.4f, 0.8f, 0.6f);
                sprintInfo.style.marginBottom = 15;
                timelineContainer.Add(sprintInfo);

                var tasksInSprint = boardData.columns.SelectMany(c => c.tasks).Where(t => t.sprintId == sp.id).ToList();
                if (tasksInSprint.Count == 0)
                {
                    timelineContainer.Add(new Label("No tasks found in this sprint.") { style = { color = new Color(0.5f, 0.5f, 0.6f) } });
                }
                else
                {
                    foreach (var t in tasksInSprint)
                    {
                        var tRow = new VisualElement();
                        tRow.style.flexDirection = FlexDirection.Row;
                        tRow.style.alignItems = Align.Center;
                        tRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
                        tRow.style.paddingTop = 10; tRow.style.paddingBottom = 10;
                        tRow.style.paddingLeft = 15; tRow.style.paddingRight = 15;
                        tRow.style.marginBottom = 5;
                        tRow.style.borderTopLeftRadius = 4; tRow.style.borderBottomLeftRadius = 4;
                        tRow.style.borderTopRightRadius = 4; tRow.style.borderBottomRightRadius = 4;

                        var tTitle = new Label(t.title);
                        tTitle.style.flexGrow = 1;
                        tTitle.style.color = Color.white;
                        tTitle.style.unityFontStyleAndWeight = FontStyle.Bold;

                        var tDates = new Label($"{t.startDate} -> {t.dueDate}");
                        tDates.style.color = new Color(0.7f, 0.7f, 0.8f);
                        tDates.style.fontSize = 11;

                        tRow.Add(tTitle);
                        tRow.Add(tDates);
                        timelineContainer.Add(tRow);
                    }
                }
            }

            sprintTimelineTab.Add(timelineContainer);
        }

        private void RefreshSprintTimelineView(string sprintTitle)
        {
            if (currentTab == TabType.SprintTimeline)
            {
                RenderSprintTimeline();
                if (sprintSelectorDropdown != null && sprintSelectorDropdown.choices.Contains(sprintTitle))
                {
                    sprintSelectorDropdown.SetValueWithoutNotify(sprintTitle);
                }
            }
        }

        private void CreateNewSprint()
        {
            var newSp = new SprintData
            {
                title = "Sprint " + ((boardData.sprints != null ? boardData.sprints.Count : 0) + 1),
                startDate = DateTime.Now.ToString("yyyy-MM-dd"),
                endDate = DateTime.Now.AddDays(14).ToString("yyyy-MM-dd")
            };
            if (boardData.sprints == null) boardData.sprints = new List<SprintData>();
            boardData.sprints.Add(newSp);
            SaveData();
            RenderSprintTimeline();
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

