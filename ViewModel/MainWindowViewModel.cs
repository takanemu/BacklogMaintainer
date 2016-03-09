
namespace BacklogMaintainer.ViewModel
{
    using CSJSONBacklog.Communicator;
    using CSJSONBacklog.Model.Projects;
    using CSJSONBacklog.Model.Space;
    using Designtime.Sourcecode.Generator.Attributes;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    [TemplateGenerateAnnotation(Name = "IsBusy", Type = typeof(bool), Comment = "処理中表示", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "UserCount", Type = typeof(int), Comment = "ユーザー数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "GroupCount", Type = typeof(int), Comment = "グループ数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "ProjectCount", Type = typeof(int), Comment = "プロジェクト数", RaisePropertyChanged = true)]
    [TemplateGenerateAnnotation(Name = "ActiveProjectCount", Type = typeof(int), Comment = "非アーカイブプロジェクト数", RaisePropertyChanged = true)]
    public partial class MainWindowViewModel : Livet.ViewModel
    {
        public string SpaceName { get; set; }
        public string APIKey { get; set; }
        public IList<UserViewModel> Users { get; private set; }
        public IList<Group> Groups { get; private set; }
        public IList<Project> InactiveProjexts { get; private set; }
        public IList<Project> RevivalProjexts { get; private set; }
        public IList<Project> DeathProjexts { get; private set; }

        public MainWindowViewModel()
        {
            this.IsBusy = false;
            this.Users = new ObservableCollection<UserViewModel>();
            this.Groups = new ObservableCollection<Group>();
            this.InactiveProjexts = new ObservableCollection<Project>();
            this.RevivalProjexts = new ObservableCollection<Project>();
            this.DeathProjexts = new ObservableCollection<Project>();
        }

        public async Task DoLoad()
        {
            // 初期化
            this.Users.Clear();
            this.Groups.Clear();
            this.InactiveProjexts.Clear();
            this.RevivalProjexts.Clear();
            this.DeathProjexts.Clear();

            // ユーザーデータ取得
            var users = await this.GetSpaceUsers();
            var userdic = new Dictionary<string, User>();
            var counter = new Dictionary<string, int>();
            var update = new Dictionary<string, IEnumerable<Activitie>>();

            foreach (var user in users)
            {
                userdic.Add(user.UserId, user);
                counter.Add(user.UserId, 0);
            }

            this.UserCount = users.Count();

            // プロジェクト登録数カウント
            var projects = await this.GetProjects();
            var projectCommunicator = new ProjectCommunicator(this.SpaceName, this.APIKey);

            this.ProjectCount = projects.Count();
            this.ActiveProjectCount = projects.Count(o => o.Archived == false);

            foreach (var project in projects)
            {
                var projectusers = await this.GetProjectUserList(projectCommunicator, project.ProjectKey);

                foreach (var user in projectusers)
                {
                    int count;

                    if (counter.TryGetValue(user.UserId, out count))
                    {
                        count++;
                        counter[user.UserId] = count;
                    }
                }
            }

            // プロジェクト未参加ユーザーの表示
            foreach (var user in users)
            {
                int count;

                if (counter.TryGetValue(user.UserId, out count))
                {
                    if (count == 0)
                    {
                        User zerouser;

                        if (userdic.TryGetValue(user.UserId, out zerouser))
                        {
                            var adduser = new UserViewModel {
                                UserId = zerouser.UserId,
                                MailAddress = zerouser.MailAddress,
                                Name = zerouser.Name,
                                Memo = "未登録",
                            };
                            this.Users.Add(adduser);
                        }
                    }
                }
            }

            // 重複ユーザーの検出
            foreach (var user in users)
            {
                var count = users.Count(o => o.MailAddress == user.MailAddress);

                if(count > 1)
                {
                    var adduser = new UserViewModel
                    {
                        UserId = user.UserId,
                        MailAddress = user.MailAddress,
                        Name = user.Name,
                        Memo = "重複登録",
                    };
                    this.Users.Add(adduser);
                }
            }

            // メンバーの居ないグループ
            var groups = await this.GetSpageGroups();

            this.GroupCount = groups.Count();

            foreach (var group in groups)
            {
                if (group.Members.Count == 0)
                {
                    this.Groups.Add(group);
                }
            }

            // 直近1ヶ月にアクセスの無いプロジェクト
            var date = DateTime.Now.AddMonths(-1);

            foreach (var project in projects)
            {
                if (project.Archived)
                {
                    continue;
                }
                IEnumerable<Activitie> activities;

                if(!update.TryGetValue(project.ProjectKey, out activities))
                {
                    activities = await this.GetProjectRecentUpdates(projectCommunicator, project.ProjectKey);
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

            // アーカイブ化されているのに、最近アクセスの有ったプロジェクト
            foreach (var project in projects)
            {
                if (project.Archived)
                {
                    IEnumerable<Activitie> activities;

                    if (!update.TryGetValue(project.ProjectKey, out activities))
                    {
                        activities = await this.GetProjectRecentUpdates(projectCommunicator, project.ProjectKey);
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

            // 1年間にアクセスの無いプロジェクト
            date = DateTime.Now.AddYears(-1);

            foreach (var project in projects)
            {
                IEnumerable<Activitie> activities;

                if (!update.TryGetValue(project.ProjectKey, out activities))
                {
                    activities = await this.GetProjectRecentUpdates(projectCommunicator, project.ProjectKey);
                    update.Add(project.ProjectKey, activities);
                }
                bool work = false;

                foreach (var activitie in activities)
                {
                    if (date < activitie.Created)
                    {
                        work = true;
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
        }

        private async Task<IEnumerable<Activitie>> GetProjectRecentUpdates(ProjectCommunicator projectCommunicator, string ProjectKey)
        {
            IEnumerable<Activitie> activities = null;

            await Task.Run(() =>
            {
                activities = projectCommunicator.GetProjectRecentUpdates(ProjectKey);
            });
            return activities;
        }

        private async Task<IEnumerable<User>> GetProjectUserList(ProjectCommunicator projectCommunicator, string ProjectKey)
        {
            IEnumerable<User> projectusers = null;

            await Task.Run(() =>
            {
                projectusers = projectCommunicator.GetProjectUserList(ProjectKey);
            });
            return projectusers;
        }

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
    }
}
