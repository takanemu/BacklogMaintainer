
namespace BacklogMaintainer.ViewModel
{
    using CSJSONBacklog.Communicator;
    using CSJSONBacklog.Model.Projects;
    using CSJSONBacklog.Model.Space;
    using BacklogMaintainer.Extensions;
    using Designtime.Sourcecode.Generator.Attributes;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Controls.Primitives;
    using System.ComponentModel;
    using CSJSONBacklog.Model.Issues;
    using Livet.Messaging;
    using Messaging;
    using MahApps.Metro.Controls.Dialogs;
    using System.IO;

    [TemplateGenerateAnnotation(Name = "IsBusy", Type = typeof(bool), Comment = "処理中表示", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "UserCount", Type = typeof(int), Comment = "ユーザー数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "GroupCount", Type = typeof(int), Comment = "グループ数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "ProjectCount", Type = typeof(int), Comment = "プロジェクト数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "ActiveProjectCount", Type = typeof(int), Comment = "非アーカイブプロジェクト数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "IsDeleteButtonDisp", Type = typeof(bool), Comment = "削除ボタン表示", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "IsDownloadButtonDisp", Type = typeof(bool), Comment = "ダウンロードボタン表示", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Kind = "Command", Name = "SelectionChanged", Comment = "タブ選択変更", CommandParameter = typeof(System.Windows.Controls.SelectionChangedEventArgs))]
    [TemplateGenerateAnnotation(Kind = "Command", Name = "Download", Comment = "添付ファイルのダウンロード")]
    [TemplateGenerateAnnotation(Kind = "Command", Name = "Delete", Comment = "削除")]
    public partial class MainWindowViewModel : Livet.ViewModel
    {
        public string SpaceName { get; set; }
        public string APIKey { get; set; }
        public IList<UserViewModel> Users { get; private set; }
        public IList<GroupViewModel> Groups { get; private set; }
        public IList<Project> InactiveProjexts { get; private set; }
        public IList<Project> RevivalProjexts { get; private set; }
        public IList<Project> DeathProjexts { get; private set; }
        private CollectionViewSource usersCollectionViewSource = new CollectionViewSource();
        private CollectionViewSource groupsCollectionViewSource = new CollectionViewSource();
        private CollectionViewSource inactiveProjextsCollectionViewSource = new CollectionViewSource();
        private CollectionViewSource revivalProjextsCollectionViewSource = new CollectionViewSource();
        private CollectionViewSource deathProjextsCollectionViewSource = new CollectionViewSource();
        private bool isProject = true;
        private bool isUser = true;
        private bool isGroup = true;
        private bool isInactive = true;
        private bool isRevival = true;
        private bool isDeath = true;
        private string selected = "users";
        private ProjectCommunicator projectCommunicator = null;
        private IEnumerable<Project> projects;
        private Dictionary<string, IEnumerable<Activitie>> projectActivities = new Dictionary<string, IEnumerable<Activitie>>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindowViewModel()
        {
            this.IsBusy = false;
            this.IsDeleteButtonDisp = false;
            this.IsDownloadButtonDisp = false;
            this.Users = new ObservableCollection<UserViewModel>();
            this.Groups = new ObservableCollection<GroupViewModel>();
            this.InactiveProjexts = new ObservableCollection<Project>();
            this.RevivalProjexts = new ObservableCollection<Project>();
            this.DeathProjexts = new ObservableCollection<Project>();

            // 複数スレッドからコレクションを操作できるようにする
            BindingOperations.EnableCollectionSynchronization(this.Users, new object());
            BindingOperations.EnableCollectionSynchronization(this.Groups, new object());
            BindingOperations.EnableCollectionSynchronization(this.InactiveProjexts, new object());
            BindingOperations.EnableCollectionSynchronization(this.RevivalProjexts, new object());
            BindingOperations.EnableCollectionSynchronization(this.DeathProjexts, new object());

            this.usersCollectionViewSource.Source = this.Users;
            this.groupsCollectionViewSource.Source = this.Groups;
            this.inactiveProjextsCollectionViewSource.Source = this.InactiveProjexts;
            this.revivalProjextsCollectionViewSource.Source = this.RevivalProjexts;
            this.deathProjextsCollectionViewSource.Source = this.DeathProjexts;
        }

        /// <summary>
        /// プロジェクトの直近の活動を取得
        /// </summary>
        /// <param name="ProjectKey"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Activitie>> GetProjectRecentUpdates(string ProjectKey)
        {
            IEnumerable<Activitie> activities = null;

            if (!this.projectActivities.TryGetValue(ProjectKey, out activities))
            {
                await Task.Run(() =>
                {
                    activities = this.projectCommunicator.GetProjectRecentUpdates(ProjectKey);
                });
                this.projectActivities.Add(ProjectKey, activities);
            }
            return activities;
        }

        /// <summary>
        /// プロジェクトメンバーの取得
        /// </summary>
        /// <param name="ProjectKey"></param>
        /// <returns></returns>
        private async Task<IEnumerable<User>> GetProjectUserList(string ProjectKey)
        {
            IEnumerable<User> projectusers = null;

            await Task.Run(() =>
            {
                projectusers = this.projectCommunicator.GetProjectUserList(ProjectKey);
            });
            return projectusers;
        }

        /// <summary>
        /// ユーザーの取得
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<User>> GetSpaceUsers()
        {
            var spaceCommunicator = new SpaceCommunicator(this.SpaceName, this.APIKey);
            IEnumerable<User> users = null;

            await Task.Run(() =>
            {
                users = spaceCommunicator.GetUserList().ToList();
            });
            return users;
        }

        /// <summary>
        /// プロジェクトの取得
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Project>> GetProjects()
        {
            var projectCommunicator = new ProjectCommunicator(this.SpaceName, this.APIKey);
            IEnumerable<Project> projects = null;

            await Task.Run(() =>
            {
                projects = projectCommunicator.GetProjectList(true).ToList();
            });
            return projects;
        }

        /// <summary>
        /// グループの取得
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<GroupViewModel>> GetSpageGroups()
        {
            var spaceCommunicator = new SpaceCommunicator(this.SpaceName, this.APIKey);
            IEnumerable<GroupViewModel> groups = null;

            await Task.Run(() =>
            {
                var param = new SpaceQuery
                {
                    Offset = 0,
                    Order = CSJSONBacklog.Model.Issues.Order.asc,
                    Count = 100,
                };
                var quary = param.GetParametersForAPI();

                groups = spaceCommunicator.GetGroupList(param).Select(x => new GroupViewModel(x)).ToList();
                //groups = spaceCommunicator.GetGroupList(param).ToList();
            });
            return groups;
        }

        public void Init()
        {
            this.projectCommunicator = new ProjectCommunicator(this.SpaceName, this.APIKey);
        }

        public void Refresh()
        {
            this.isProject = true;
            this.isUser = true;
            this.isGroup = true;
            this.isInactive = true;
            this.isRevival = true;
            this.isDeath = true;
            this.projectActivities.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public ICollectionView UsersView
        {
            get
            {
                return this.usersCollectionViewSource.View;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ICollectionView GroupsView
        {
            get
            {
                return this.groupsCollectionViewSource.View;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ICollectionView InactiveProjextsView
        {
            get
            {
                return this.inactiveProjextsCollectionViewSource.View;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ICollectionView RevivalProjextsView
        {
            get
            {
                return this.revivalProjextsCollectionViewSource.View;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ICollectionView DeathProjextsView
        {
            get
            {
                return this.deathProjextsCollectionViewSource.View;
            }
        }

        public async Task Project()
        {
            if (this.isProject)
            {
                this.projects = await this.GetProjects();
                this.ProjectCount = this.projects.Count();
                this.ActiveProjectCount = this.projects.Count(o => o.Archived == false);
                this.isProject = false;
            }
        }

        /// <summary>
        /// ユーザー一覧作成
        /// </summary>
        /// <returns></returns>
        public async Task User()
        {
            if (this.isUser)
            {
                // 初期化
                this.Users.Clear();
                // ユーザーデータ取得
                var users = await this.GetSpaceUsers();
                var userdic = new Dictionary<string, User>();
                var counter = new Dictionary<string, int>();

                users.ForEach(user =>
                {
                    userdic.Add(user.UserId, user);
                    counter.Add(user.UserId, 0);
                });
                this.UserCount = users.Count();

                // プロジェクト登録数カウント
                await this.Project();

                foreach (var project in this.projects)
                {
                    var projectusers = await this.GetProjectUserList(project.ProjectKey);

                    projectusers.ForEach(user =>
                    {
                        int count;

                        if (counter.TryGetValue(user.UserId, out count))
                        {
                            count++;
                            counter[user.UserId] = count;
                        }
                    });
                }

                // プロジェクト未参加ユーザーの表示
                users.ForEach(user =>
                {
                    int count;

                    if (counter.TryGetValue(user.UserId, out count))
                    {
                        if (count == 0)
                        {
                            User zerouser;

                            if (userdic.TryGetValue(user.UserId, out zerouser))
                            {
                                var adduser = new UserViewModel
                                {
                                    Id = user.Id,
                                    UserId = zerouser.UserId,
                                    MailAddress = zerouser.MailAddress,
                                    Name = zerouser.Name,
                                    Memo = "未登録",
                                };
                                this.Users.Add(adduser);
                            }
                        }
                    }
                });

                // 重複ユーザーの検出
                users = users.Where(user => users.Count(o => o.MailAddress == user.MailAddress) > 1).Select(user => new UserViewModel
                {
                    Id = user.Id,
                    UserId = user.UserId,
                    MailAddress = user.MailAddress,
                    Name = user.Name,
                    Memo = "重複登録",
                });
                this.Users.Concat(users);
                this.isUser = false;
            }
        }

        /// <summary>
        /// グループ一覧作成
        /// </summary>
        /// <returns></returns>
        public async Task Group()
        {
            if (this.isGroup)
            {
                this.Groups.Clear();
                var groups = await this.GetSpageGroups();

                this.GroupCount = groups.Count();
                groups = groups.Where(o => o.Members.Count == 0);

                foreach (var group in groups)
                {
                    this.Groups.Add(group);
                }
                this.isGroup = false;
            }
        }

        public async Task Inactive()
        {
            if (this.isInactive)
            {
                this.InactiveProjexts.Clear();

                var update = new Dictionary<string, IEnumerable<Activitie>>();
                await this.Project();
                var date = DateTime.Now.AddMonths(-1);

                foreach (var project in this.projects)
                {
                    if (project.Archived)
                    {
                        continue;
                    }
                    IEnumerable<Activitie> activities;

                    if (!update.TryGetValue(project.ProjectKey, out activities))
                    {
                        activities = await this.GetProjectRecentUpdates(project.ProjectKey);
                        update.Add(project.ProjectKey, activities);
                    }
                    bool work = false;

                    foreach (var activitie in activities)
                    {
                        if (date < activitie.Created)
                        {
                            work = true;
                            break;
                        }
                    }
                    if (!work)
                    {
                        this.InactiveProjexts.Add(project);
                        //if (activities.Count() > 1)
                        //{
                        //    Console.WriteLine(activities.First());
                        //}
                    }
                }
                this.isInactive = false;
            }
        }

        public async Task Revival()
        {
            if (this.isRevival)
            {
                this.RevivalProjexts.Clear();

                var date = DateTime.Now.AddMonths(-1);
                var update = new Dictionary<string, IEnumerable<Activitie>>();

                await this.Project();

                foreach (var project in this.projects)
                {
                    if (project.Archived)
                    {
                        IEnumerable<Activitie> activities;

                        if (!update.TryGetValue(project.ProjectKey, out activities))
                        {
                            activities = await this.GetProjectRecentUpdates(project.ProjectKey);
                            update.Add(project.ProjectKey, activities);
                        }
                        bool work = false;

                        foreach (var activitie in activities)
                        {
                            if (date < activitie.Created)
                            {
                                work = true;
                                break;
                            }
                        }
                        if (work)
                        {
                            this.RevivalProjexts.Add(project);
                            //if (activities.Count() > 1)
                            //{
                            //    Console.WriteLine(activities.First());
                            //}
                        }
                    }
                }
                this.isRevival = false;
            }
        }

        public async Task Death()
        {
            if (this.isDeath)
            {
                this.DeathProjexts.Clear();

                var date = DateTime.Now.AddYears(-1);
                var update = new Dictionary<string, IEnumerable<Activitie>>();

                await this.Project();

                foreach (var project in this.projects)
                {
                    IEnumerable<Activitie> activities;

                    if (!update.TryGetValue(project.ProjectKey, out activities))
                    {
                        activities = await this.GetProjectRecentUpdates(project.ProjectKey);
                        update.Add(project.ProjectKey, activities);
                    }
                    bool work = false;

                    foreach (var activitie in activities)
                    {
                        if (date < activitie.Created)
                        {
                            work = true;
                            break;
                        }
                    }
                    if (!work)
                    {
                        this.DeathProjexts.Add(project);
                        //if (activities.Count() > 1)
                        //{
                        //    Console.WriteLine(activities.First());
                        //}
                    }
                }
                this.isDeath = false;
            }

        }

        public async Task Update()
        {
            try
            {
                switch (this.selected)
                {
                    case "users":
                        await this.User();
                        this.IsDeleteButtonDisp = true;
                        this.IsDownloadButtonDisp = false;
                        break;
                    case "groups":
                        await this.Group();
                        this.IsDeleteButtonDisp = true;
                        this.IsDownloadButtonDisp = false;
                        break;
                    case "inactive":
                        await this.Inactive();
                        this.IsDeleteButtonDisp = false;
                        this.IsDownloadButtonDisp = true;
                        break;
                    case "revival":
                        await this.Revival();
                        this.IsDeleteButtonDisp = false;
                        this.IsDownloadButtonDisp = true;
                        break;
                    case "death":
                        await this.Death();
                        this.IsDeleteButtonDisp = false;
                        this.IsDownloadButtonDisp = true;
                        break;
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// タブ選択変更時処理
        /// </summary>
        /// <param name="e"></param>
        private void SelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var tabItem = e.AddedItems[0] as TabItem;

                if (tabItem != null)
                {
                    var header = tabItem.Header as string;

                    Task.Run(async () =>
                    {
                        this.IsBusy = true;
                        this.selected = header;
                        await this.Update();
                        this.IsBusy = false;
                    });
                }
            }
        }

        /// <summary>
        /// 添付ファイルのダウンロード処理
        /// </summary>
        private async void Download()
        {
            this.IsBusy = true;

            Project selectedItem = null;

            switch (this.selected)
            {
                case "inactive":
                    selectedItem = this.InactiveProjextsView.CurrentItem as Project;
                    break;
                case "revival":
                    selectedItem = this.RevivalProjextsView.CurrentItem as Project;
                    break;
                case "death":
                    selectedItem = this.DeathProjextsView.CurrentItem as Project;
                    break;
            }
            await Task.Run(() =>
            {
                if (selectedItem != null)
                {
                    IssueCommunicator issue = new IssueCommunicator(this.SpaceName, this.APIKey);
                    var count = issue.GetIssuesCount(selectedItem.Id);

                    if (count > 0)
                    {
                        int index = 0;
                        int down = 100;
                        IEnumerable<Issue> list = new List<Issue>();

                        do
                        {
                            var param = new IssueQuery
                            {
                                ProjectIds = new List<int> { selectedItem.Id, },
                                ParentChild = 0,
                                Attachment = true,
                                SharedFile = false,
                                Sort = Sort.Attachment,
                                Offset = index,
                                Count = 100,
                            };
                            var issues = issue.GetIssues(param);

                            list = list.Concat(issues);
                            down = issues.Count();
                            index += down;
                        }
                        while (down >= 100);

                        foreach (var item in list)
                        {
                            if (item.attachments.Count > 0)
                            {
                                foreach (var file in item.attachments)
                                {
                                    var path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                                    var key = item.issueKey;
                                    var name = file.name;
                                    char[] invalidChars = Path.GetInvalidFileNameChars();

                                    if (name.IndexOfAny(invalidChars) < 0)
                                    {
                                        //Console.WriteLine("ファイル名に使用できない文字は使われていません。");
                                    }
                                    else
                                    {
                                        //Console.WriteLine("ファイル名に使用できない文字が使われています。");
                                        foreach (char c in invalidChars)
                                        {
                                            name = name.Replace(c, '_');
                                        }
                                    }
                                    issue.DownloadAttachmentFile(path, key, name, item.id, file.id);
                                }
                            }
                        }
                    }
                }
            });

            this.IsBusy = false;
        }

        /// <summary>
        /// 削除機能
        /// </summary>
        private void Delete()
        {
            if (this.selected == "users")
            {
                var message = new MetroWindowConfirmationMessage("ユーザーを削除してよろしいですか？", "削除確認", MessageDialogStyle.AffirmativeAndNegative,
                    async (MessageDialogResult result) =>
                    {
                        if(result == MessageDialogResult.Affirmative)
                        {
                            await this.UserDelete();
                        }
                    },
                    "Confirm");
                
                Messenger.GetResponse(message);
            }
            else if (this.selected == "groups")
            {
                var message = new MetroWindowConfirmationMessage("グループを削除してよろしいですか？", "削除確認", MessageDialogStyle.AffirmativeAndNegative,
                    async (MessageDialogResult result) =>
                    {
                        if (result == MessageDialogResult.Affirmative)
                        {
                            await this.GroupDelete();
                        }
                    },
                    "Confirm");

                Messenger.GetResponse(message);
            }
        }

        /// <summary>
        /// ユーザー削除
        /// </summary>
        private async Task UserDelete()
        {
            var selected = this.Users.Where(x => x.IsSelected).ToList();

            if (selected.Count() > 0)
            {
                this.IsBusy = true;

                var spaceCommunicator = new SpaceCommunicator(this.SpaceName, this.APIKey);

                await Task.Run(() =>
                {
                    foreach (var user in selected)
                    {
                        // ユーザー削除
                        spaceCommunicator.DeleteUser(user.Id);
                        // リストから削除
                        this.Users.Remove(user);
                    }
                });
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// グループ削除
        /// </summary>
        private async Task GroupDelete()
        {
            var selected = this.Groups.Where(x => x.IsSelected).ToList();

            if (selected.Count() > 0)
            {
                this.IsBusy = true;

                var spaceCommunicator = new SpaceCommunicator(this.SpaceName, this.APIKey);

                await Task.Run(() =>
                {
                    foreach (var group in selected)
                    {
                        // グループ削除
                        spaceCommunicator.DeleteGroup(group.Id);
                        // リストから削除
                        this.Groups.Remove(group);
                    }
                });
                this.IsBusy = false;
            }
        }
    }
}
