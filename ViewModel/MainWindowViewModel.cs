
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

    [TemplateGenerateAnnotation(Name = "IsBusy", Type = typeof(bool), Comment = "処理中表示", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "UserCount", Type = typeof(int), Comment = "ユーザー数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "GroupCount", Type = typeof(int), Comment = "グループ数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "ProjectCount", Type = typeof(int), Comment = "プロジェクト数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "ActiveProjectCount", Type = typeof(int), Comment = "非アーカイブプロジェクト数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Kind = "Command", Name = "SelectionChanged", Comment = "タブ選択変更", CommandParameter = typeof(System.Windows.Controls.SelectionChangedEventArgs))]
    public partial class MainWindowViewModel : Livet.ViewModel
    {
        public string SpaceName { get; set; }
        public string APIKey { get; set; }
        public IList<UserViewModel> Users { get; private set; }
        public IList<Group> Groups { get; private set; }
        public IList<Project> InactiveProjexts { get; private set; }
        public IList<Project> RevivalProjexts { get; private set; }
        public IList<Project> DeathProjexts { get; private set; }
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
            this.Users = new ObservableCollection<UserViewModel>();
            this.Groups = new ObservableCollection<Group>();
            this.InactiveProjexts = new ObservableCollection<Project>();
            this.RevivalProjexts = new ObservableCollection<Project>();
            this.DeathProjexts = new ObservableCollection<Project>();
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
                projects = projectCommunicator.GetProjectList().ToList();
            });
            return projects;
        }

        /// <summary>
        /// グループの取得
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Group>> GetSpageGroups()
        {
            var spaceCommunicator = new SpaceCommunicator(this.SpaceName, this.APIKey);
            IEnumerable<Group> groups = null;

            await Task.Run(() =>
            {
                var param = new SpaceQuery {
                    Offset = 0,
                    Order = CSJSONBacklog.Model.Issues.Order.asc,
                    Count = 100,
                };
                var quary = param.GetParametersForAPI();

                groups = spaceCommunicator.GetGroupList(param).ToList();
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

        public async Task User()
        {
            if (this.isUser)
            {
                // 初期化
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Users.Clear();
                });
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
                                    UserId = zerouser.UserId,
                                    MailAddress = zerouser.MailAddress,
                                    Name = zerouser.Name,
                                    Memo = "未登録",
                                };
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    this.Users.Add(adduser);
                                });
                            }
                        }
                    }
                });

                // 重複ユーザーの検出
                users = users.Where(user => users.Count(o => o.MailAddress == user.MailAddress) > 1).Select(user => new UserViewModel
                {
                    UserId = user.UserId,
                    MailAddress = user.MailAddress,
                    Name = user.Name,
                    Memo = "重複登録",
                });
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Users.Concat(users);
                });
                this.isUser = false;
            }
        }

        public async Task Group()
        {
            if(this.isGroup)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Groups.Clear();
                });
                var groups = await this.GetSpageGroups();

                this.GroupCount = groups.Count();
                groups = groups.Where(o => o.Members.Count == 0);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Groups = groups.ToList();
                });
                this.isGroup = false;
            }
        }

        public async Task Inactive()
        {
            if(this.isInactive)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.InactiveProjexts.Clear();
                });
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
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.InactiveProjexts.Add(project);
                        });
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
            if(this.isRevival)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.RevivalProjexts.Clear();
                });
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
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                this.RevivalProjexts.Add(project);
                            });
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
            if(this.isDeath)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.DeathProjexts.Clear();
                });
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
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.DeathProjexts.Add(project);
                        });
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
                        break;
                    case "groups":
                        await this.Group();
                        break;
                    case "inactive":
                        await this.Inactive();
                        break;
                    case "revival":
                        await this.Revival();
                        break;
                    case "death":
                        await this.Death();
                        break;
                }
            }
            catch(Exception ex)
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
    }
}
