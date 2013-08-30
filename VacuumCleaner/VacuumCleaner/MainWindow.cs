using System.Collections.Generic;
using TomShane.Neoforce.Controls;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.IO;
using VacuumCleaner.Env;
using VacuumCleaner.Agents;
using System.Text.RegularExpressions;
using System.Threading;

namespace VacuumCleaner
{
    public class MainWindow : Window
    {
        private enum RunStatistics
        {
            Run = 0,
            TimeStep,
            Action,
            DirtyOnMap,
            OverallDirty,
            CleanedDirty,
            ConsumedEnergy
        };

        private enum TotalStatistics
        {
            CompletedRuns = 0,
            TotalDirtyDegree,
            TotalCleanedDirty,
            TotalConsumedEnergy,
            AverageDirtyDegree,
            AverageCleanedDirty,
            AverageConsumedEnergy
        };

        private class MapWindow : Control
        {
            #region Declarations

            public Environment Environment;
            public IEnvironmentRenderer Renderer;
            public float PosLerp = 0f;

            #endregion

            #region Methods

            public MapWindow(Manager manager) : base(manager) { }

            protected override void DrawControl(Renderer renderer, Rectangle rect, GameTime gameTime)
            {
                if (Environment != null && Renderer != null)
                {
                    Renderer.Render(renderer.SpriteBatch, Environment, gameTime, DrawingRect, PosLerp);
                }
            }

            public void DrawP(Renderer renderer, Rectangle rect, GameTime gameTime)
            {
                if (Environment != null && Renderer != null)
                {
                    Renderer.Render(renderer.SpriteBatch, Environment, gameTime, DrawingRect, PosLerp);
                }
            }

            protected override void Update(GameTime gameTime)
            {
                if (Environment != null && Renderer != null)
                    Renderer.Update(gameTime);
            }

            #endregion
        }

        #region Declarations

        private const string FOLDER_NOTATION = "[. . .]";
        private const string UP_FOLDER_NOTATION = "...";
        private const string MAP_EXTENSION = ".map";
        private const int MIN_DISPLAY_TIME = 20;

        private TextBox StepsTxt;
        private TextBox TimeTxt;
        private TextBox LifeTimeTxt;
        private TextBox TestCaseTxt;
        private Button DoOneStepBtn;
        private Button DoOneRunBtn;
        private Button NextRunBtn;
        private Button DoAllRunBtn;
        private Button DisplayBtn;
        private Button SelectMapBtn;
        private Button NewMapBtn;
        private GroupBox GrpBox;
        private SideBar SideBar;
        private SideBar SideBarRight;
        private Dialog OpenFileDialog;
        private Dialog NewGameDialog;
        private ListBox FilesList;
        private ExitDialog ExitDlg;
        private ComboBox AgentsComboBox;
        private ComboBox RenderersComboBox;
        private Label AgentLbl;
        private Label LifeTimeLbl;
        private Label TestCaseLbl;

        private bool Displaying = false;
        private int currentRun = 0;
        private int DisplayTime = MIN_DISPLAY_TIME;
        private int DisplaySteps = 100;
        private int TotalStepsDone = 0;
        private int StepsDone = 0;
        private double TimeSinceStep = 0.0f;
        private int LifeTime = 0;
        private int TestCase = 0;

        private int CurrentRun
        {
            get { return currentRun; }
            set 
            { 
                currentRun = value;
                RunStatsLbls[(int)RunStatistics.Run].Text = value.ToString();
                TotalStatsLbls[(int)TotalStatistics.CompletedRuns].Text = value.ToString();
            }
        }

        private Environment Environment = null;
        private Evaluator Evaluator = new Evaluator();
        private RandomNumGen RNG;
        private string SelectedMap;
        private MainWindow.MapWindow MapControl;
        private Application ThisApp;
        private bool EnvironmentChanged = false;
        private Texture2D Background;
        private SpriteBatch SpriteBatch;
        private ContentManager ContentManager;
        private List<IEnvironmentRenderer> Renderers = new List<IEnvironmentRenderer>();
        private IAgent CurrentAgent;
        private string AgentName;
        private Regex Rgx = new Regex("^[0-9]+", RegexOptions.Compiled);
        private List<Label> RunStatsLbls = new List<Label>();
        private List<Label> TotalStatsLbls = new List<Label>();
        
        private long overallDirty;
        private long consumedEnergy;
        private long dirtyOnMap;
        private long cleanedDirty;

        private long OverallDirty
        {
            get { return overallDirty; }
            set { overallDirty = value; RunStatsLbls[(int)RunStatistics.OverallDirty].Text = value.ToString(); }
        }

        private long ConsumedEnergy
        {
            get { return consumedEnergy; }
            set { consumedEnergy = value; RunStatsLbls[(int)RunStatistics.ConsumedEnergy].Text = value.ToString(); }
        }

        private long DirtyOnMap
        {
            get { return dirtyOnMap; }
            set { dirtyOnMap = value; RunStatsLbls[(int)RunStatistics.DirtyOnMap].Text = value.ToString(); }
        }

        private long CleanedDirty
        {
            get { return cleanedDirty; }
            set { cleanedDirty = value; RunStatsLbls[(int)RunStatistics.CleanedDirty].Text = value.ToString(); }
        }

        private long totalDirtyDegree = 0;
        private long totalConsumedEnergy = 0;
        private long totalCleanedDirty = 0;

        private long TotalDirtyDegree
        {
            get { return totalDirtyDegree; }
            set 
            { 
                totalDirtyDegree = value;
                TotalStatsLbls[(int)TotalStatistics.TotalDirtyDegree].Text = value.ToString();
                TotalStatsLbls[(int)TotalStatistics.AverageDirtyDegree].Text = ((float)value / (CurrentRun + 1)).ToString();
            }
        }

        private long TotalConsumedEnergy
        {
            get { return totalConsumedEnergy; }
            set 
            { 
                totalConsumedEnergy = value;
                TotalStatsLbls[(int)TotalStatistics.TotalConsumedEnergy].Text = value.ToString();
                TotalStatsLbls[(int)TotalStatistics.AverageConsumedEnergy].Text = ((float)value / (CurrentRun + 1)).ToString();
            }
        }

        private long TotalCleanedDirty
        {
            get { return totalCleanedDirty; }
            set { 
                totalCleanedDirty = value;
                TotalStatsLbls[(int)TotalStatistics.TotalCleanedDirty].Text = value.ToString();
                TotalStatsLbls[(int)TotalStatistics.AverageCleanedDirty].Text = ((float)value / (CurrentRun + 1)).ToString();
            }
        }

        #endregion

        #region Methods

        public MainWindow(Manager manager, Application thisApp)
            : base(manager)
        {
            Background = Manager.Content.Load<Texture2D>("Content\\Textures\\background");
            (Manager.Game as Application).BackgroundImage = Background;
            ContentManager = new ContentManager(manager.Game.Services, "Content");
            SpriteBatch = new SpriteBatch(manager.GraphicsDevice);
            Renderers.Add(new Environment.TileMap2D(ContentManager));
            Renderers.Add(new Environment.TileMap3D(Manager.GraphicsDevice, ContentManager, Height, Height));
            this.ThisApp = thisApp;
            AutoScroll = false;
     
            InitControls();
        }

        /// <summary>
        /// Initializes UI elements
        /// </summary>
        private void InitControls()
        {
            //Map

            MapControl = new MapWindow(Manager);
            MapControl.Init();
            MapControl.SetPosition(201, 0);
            MapControl.Environment = Environment;
            MapControl.Renderer = Renderers[0];
            MapControl.SetSize(Height, Height);
            MapControl.Resizable = true;
            MapControl.Movable = true;
            Add(MapControl);

            //Left side bar

            SideBar = new SideBar(Manager);
            SideBar.Init();
            SideBar.StayOnBack = true;
            SideBar.Passive = true;
            SideBar.SetSize(200, Height);
            SideBar.Anchor = Anchors.Left | Anchors.Top | Anchors.Bottom;

            Add(SideBar);

            NewMapBtn = new Button(Manager);
            NewMapBtn.Init();
            NewMapBtn.Text = "New Map";
            NewMapBtn.SetPosition(25, 10);
            NewMapBtn.SetSize(150, 25);
            NewMapBtn.Anchor = Anchors.Top | Anchors.Left;
            NewMapBtn.Click += new EventHandler(NewMapBtn_Click);
            SideBar.Add(NewMapBtn);

            SelectMapBtn = new Button(Manager);
            SelectMapBtn.Init();
            SelectMapBtn.Text = "Select Map";
            SelectMapBtn.Click += new EventHandler(selectMapBtn_Click);
            SelectMapBtn.SetPosition(25, 40);
            SelectMapBtn.SetSize(150, 25);
            SelectMapBtn.Anchor = Anchors.Top | Anchors.Left;
            SideBar.Add(SelectMapBtn);

            DoOneStepBtn = new Button(Manager);
            DoOneStepBtn.Init();
            DoOneStepBtn.Text = "Do One Step";
            DoOneStepBtn.SetPosition(25, 90);
            DoOneStepBtn.SetSize(150, 25);
            DoOneStepBtn.Anchor = Anchors.Top | Anchors.Left;
            DoOneStepBtn.Click += new EventHandler(DoOneStepBtn_Click);
            SideBar.Add(DoOneStepBtn);

            DoOneRunBtn = new Button(Manager);
            DoOneRunBtn.Init();
            DoOneRunBtn.Text = "Do One Run";
            DoOneRunBtn.SetPosition(25, 120);
            DoOneRunBtn.SetSize(150, 25);
            DoOneRunBtn.Anchor = Anchors.Top | Anchors.Left;
            DoOneRunBtn.Click += new EventHandler(DoOneRunBtn_Click);
            SideBar.Add(DoOneRunBtn);

            NextRunBtn = new Button(Manager);
            NextRunBtn.Init();
            NextRunBtn.Text = "Next Run";
            NextRunBtn.SetPosition(25, 150);
            NextRunBtn.SetSize(150, 25);
            NextRunBtn.Anchor = Anchors.Top | Anchors.Left;
            NextRunBtn.Click += new EventHandler(NextRunBtn_Click);
            SideBar.Add(NextRunBtn);

            DoAllRunBtn = new Button(Manager);
            DoAllRunBtn.Init();
            DoAllRunBtn.Text = "Do All Run";
            DoAllRunBtn.SetPosition(25, 180);
            DoAllRunBtn.SetSize(150, 25);
            DoAllRunBtn.Anchor = Anchors.Top | Anchors.Left;
            DoAllRunBtn.Click += new EventHandler(DoAllRunBtn_Click);
            SideBar.Add(DoAllRunBtn);

            GrpBox = new GroupBox(Manager);
            GrpBox.Init();
            GrpBox.SetSize(SideBar.Width - 30, 135);
            GrpBox.SetPosition(5, 220);
            GrpBox.ClientWidth = 180;
            GrpBox.Text = "Display Options";
            GrpBox.TextColor = Color.Wheat;
            SideBar.Add(GrpBox);

            Label stepsLbl = new Label(Manager);
            stepsLbl.Init();
            stepsLbl.Text = "Steps:";
            stepsLbl.SetPosition(5, 25);
            GrpBox.Add(stepsLbl);

            StepsTxt = new TextBox(Manager);
            StepsTxt.Init();
            StepsTxt.Text = "100";
            StepsTxt.SetSize(100, 25);
            StepsTxt.SetPosition(5 + stepsLbl.Width, 20);
            StepsTxt.TextColor = Color.WhiteSmoke;
            StepsTxt.TextChanged += new EventHandler(TimeTxt_TextChanged);
            GrpBox.Add(StepsTxt);

            Label timeLbl = new Label(Manager);
            timeLbl.Init();
            timeLbl.Text = "Time:";
            timeLbl.SetPosition(5, 60);
            GrpBox.Add(timeLbl);

            TimeTxt = new TextBox(Manager);
            TimeTxt.Init();
            TimeTxt.Text = "20";
            TimeTxt.SetSize(100, 25);
            TimeTxt.SetPosition(5 + stepsLbl.Width, 55);
            TimeTxt.TextChanged += new EventHandler(TimeTxt_TextChanged);
            TimeTxt.TextColor = Color.WhiteSmoke;
            GrpBox.Add(TimeTxt);

            DisplayBtn = new Button(Manager);
            DisplayBtn.Init();
            DisplayBtn.Text = "Display";
            DisplayBtn.Click += new EventHandler(displayBtn_Click);
            DisplayBtn.SetSize(120, 25);
            DisplayBtn.SetPosition(50, 90);
            GrpBox.Add(DisplayBtn);

            Label rendererLbl = new Label(Manager);
            rendererLbl.Init();
            rendererLbl.Text = "Renderer:";
            rendererLbl.SetPosition(25, 360);
            Add(rendererLbl);

            RenderersComboBox = new ComboBox(Manager);
            RenderersComboBox.Init();
            RenderersComboBox.SetPosition(25, 385);
            RenderersComboBox.Width = 150;
            RenderersComboBox.TextColor = Color.Wheat;
            RenderersComboBox.Items.AddRange(Renderers);
            RenderersComboBox.ItemIndex = 0;
            RenderersComboBox.TextChanged += new EventHandler(RenderersComboBox_TextChanged);
            Add(RenderersComboBox);

            Button makeFullScreenBtn = new Button(Manager);
            makeFullScreenBtn.Init();
            makeFullScreenBtn.Text = "Fullscreen";
            makeFullScreenBtn.SetPosition(25, 475);
            makeFullScreenBtn.SetSize(150, 25);
            makeFullScreenBtn.Click += new EventHandler(makeFullScreenBtn_Click);
            makeFullScreenBtn.Anchor = Anchors.Top | Anchors.Left;
            SideBar.Add(makeFullScreenBtn);

            Button exitBtn = new Button(Manager);
            exitBtn.Init();
            exitBtn.Text = "Quit";
            exitBtn.SetPosition(25, 510);
            exitBtn.SetSize(150, 25);
            exitBtn.Click += new EventHandler(exitBtn_Click);
            exitBtn.Anchor = Anchors.Top | Anchors.Left;
            SideBar.Add(exitBtn);

            ExitDlg = new ExitDialog(Manager);
            ExitDlg.Init();
            ExitDlg.Closed += new WindowClosedEventHandler(ExitDlg_Closed);
            Manager.Add(ExitDlg);
            ExitDlg.Hide();

            //Right side bar
            SideBarRight = new SideBar(Manager);
            SideBarRight.Init();
            SideBarRight.StayOnBack = true;
            SideBarRight.Resizable = true;
            SideBarRight.ResizeEdge = Anchors.Left;
            SideBarRight.SetSize(200, Height);
            SideBarRight.SetPosition(Width - SideBarRight.Width, 0);
            SideBarRight.Anchor = Anchors.Right | Anchors.Top | Anchors.Bottom;
            Add(SideBarRight);

            GroupBox runParamsGrpBox = new GroupBox(Manager);
            runParamsGrpBox.Init();
            runParamsGrpBox.SetSize(SideBarRight.Width - 30, 155);
            runParamsGrpBox.SetPosition(5, 10);
            runParamsGrpBox.ClientWidth = 38;
            runParamsGrpBox.Text = "Run Statistics";
            runParamsGrpBox.TextColor = Color.Wheat;
            runParamsGrpBox.Anchor = Anchors.Left | Anchors.Right | Anchors.Top;
            SideBarRight.Add(runParamsGrpBox);

            Label runLbl1 = new Label(Manager);
            runLbl1.Init();
            runLbl1.Text = "Run:";
            runLbl1.SetPosition(10, 20);
            runLbl1.Width = 120;
            runParamsGrpBox.Add(runLbl1);

            Label runLbl2 = new Label(Manager);
            runLbl2.Init();
            runLbl2.Text = "0";
            runLbl2.SetPosition(runLbl1.Left + runLbl1.Width + 10, 20);
            runParamsGrpBox.Add(runLbl2);
            RunStatsLbls.Add(runLbl2);

            Label timeStepLbl1 = new Label(Manager);
            timeStepLbl1.Init();
            timeStepLbl1.Text = "Time step:";
            timeStepLbl1.SetPosition(10, 35);
            timeStepLbl1.Width = 120;
            runParamsGrpBox.Add(timeStepLbl1);

            Label timeStepLbl2 = new Label(Manager);
            timeStepLbl2.Init();
            timeStepLbl2.Text = "0";
            timeStepLbl2.SetPosition(timeStepLbl1.Left + timeStepLbl1.Width + 10, 35);
            runParamsGrpBox.Add(timeStepLbl2);
            RunStatsLbls.Add(timeStepLbl2);

            Label actionLbl1 = new Label(Manager);
            actionLbl1.Init();
            actionLbl1.Text = "Action:";
            actionLbl1.SetPosition(10, 60);
            actionLbl1.Width = 120;
            runParamsGrpBox.Add(actionLbl1);

            Label actionLbl2 = new Label(Manager);
            actionLbl2.Init();
            actionLbl2.Text = "0";
            actionLbl2.SetPosition(actionLbl1.Left + actionLbl1.Width + 10, 60);
            runParamsGrpBox.Add(actionLbl2);
            RunStatsLbls.Add(actionLbl2);

            Label dirtyOnMapLbl1 = new Label(Manager);
            dirtyOnMapLbl1.Init();
            dirtyOnMapLbl1.Text = "Dirty on map:";
            dirtyOnMapLbl1.SetPosition(10, 85);
            dirtyOnMapLbl1.Width = 120;
            runParamsGrpBox.Add(dirtyOnMapLbl1);

            Label dirtyOnMapLbl2 = new Label(Manager);
            dirtyOnMapLbl2.Init();
            dirtyOnMapLbl2.Text = "0";
            dirtyOnMapLbl2.SetPosition(dirtyOnMapLbl1.Left + dirtyOnMapLbl1.Width + 10, 85);
            runParamsGrpBox.Add(dirtyOnMapLbl2);
            RunStatsLbls.Add(dirtyOnMapLbl2);

            Label overAllDirtyLbl1 = new Label(Manager);
            overAllDirtyLbl1.Init();
            overAllDirtyLbl1.Text = "Overall dirty:";
            overAllDirtyLbl1.SetPosition(10, 100);
            overAllDirtyLbl1.Width = 120;
            runParamsGrpBox.Add(overAllDirtyLbl1);

            Label overAllDirtyLbl2 = new Label(Manager);
            overAllDirtyLbl2.Init();
            overAllDirtyLbl2.Text = "0";
            overAllDirtyLbl2.SetPosition(overAllDirtyLbl1.Left + overAllDirtyLbl1.Width + 10, 100);
            runParamsGrpBox.Add(overAllDirtyLbl2);
            RunStatsLbls.Add(overAllDirtyLbl2);

            Label cleanedDirtyLbl1 = new Label(Manager);
            cleanedDirtyLbl1.Init();
            cleanedDirtyLbl1.Text = "Cleaned dirty:";
            cleanedDirtyLbl1.SetPosition(10, 115);
            cleanedDirtyLbl1.Width = 120;
            runParamsGrpBox.Add(cleanedDirtyLbl1);

            Label cleanedDirtyLbl2 = new Label(Manager);
            cleanedDirtyLbl2.Init();
            cleanedDirtyLbl2.Text = "0";
            cleanedDirtyLbl2.SetPosition(cleanedDirtyLbl1.Left + cleanedDirtyLbl1.Width + 10, 115);
            runParamsGrpBox.Add(cleanedDirtyLbl2);
            RunStatsLbls.Add(cleanedDirtyLbl2);

            Label consumedEnergyLbl1 = new Label(Manager);
            consumedEnergyLbl1.Init();
            consumedEnergyLbl1.Text = "Consumed energy:";
            consumedEnergyLbl1.SetPosition(10, 130);
            consumedEnergyLbl1.Width = 120;
            runParamsGrpBox.Add(consumedEnergyLbl1);

            Label consumedEnergyLbl2 = new Label(Manager);
            consumedEnergyLbl2.Init();
            consumedEnergyLbl2.Text = "0";
            consumedEnergyLbl2.SetPosition(consumedEnergyLbl1.Left + consumedEnergyLbl1.Width + 10, 130);
            runParamsGrpBox.Add(consumedEnergyLbl2);
            RunStatsLbls.Add(consumedEnergyLbl2);
            ////
            GroupBox totalParamsGrpBox = new GroupBox(Manager);
            totalParamsGrpBox.Init();
            totalParamsGrpBox.SetSize(SideBarRight.Width - 30, 215);
            totalParamsGrpBox.SetPosition(5, runParamsGrpBox.Top + runParamsGrpBox.Height + 10);
            totalParamsGrpBox.ClientWidth = 38;
            totalParamsGrpBox.Text = "Total Statistics";
            totalParamsGrpBox.TextColor = Color.Wheat;
            totalParamsGrpBox.Anchor = Anchors.Left | Anchors.Right | Anchors.Top;
            SideBarRight.Add(totalParamsGrpBox);

            Label completedRunsLbl1 = new Label(Manager);
            completedRunsLbl1.Init();
            completedRunsLbl1.Text = "Completed Runs:";
            completedRunsLbl1.SetPosition(10, 20);
            completedRunsLbl1.Width = 120;
            totalParamsGrpBox.Add(completedRunsLbl1);

            Label completedRunsLbl2 = new Label(Manager);
            completedRunsLbl2.Init();
            completedRunsLbl2.Text = "0";
            completedRunsLbl2.SetPosition(completedRunsLbl1.Left + completedRunsLbl1.Width + 10, 20);
            totalParamsGrpBox.Add(completedRunsLbl2);
            TotalStatsLbls.Add(completedRunsLbl2);

            Label totalDirtyLbl1 = new Label(Manager);
            totalDirtyLbl1.Init();
            totalDirtyLbl1.Text = "Total dirty degree:";
            totalDirtyLbl1.SetPosition(10, 35);
            totalDirtyLbl1.Width = 120;
            totalParamsGrpBox.Add(totalDirtyLbl1);

            Label totalDirtyLbl2 = new Label(Manager);
            totalDirtyLbl2.Init();
            totalDirtyLbl2.Text = "0";
            totalDirtyLbl2.SetPosition(totalDirtyLbl1.Left + totalDirtyLbl1.Width + 10, 35);
            totalParamsGrpBox.Add(totalDirtyLbl2);
            TotalStatsLbls.Add(totalDirtyLbl2);

            Label totalCleanedLbl1 = new Label(Manager);
            totalCleanedLbl1.Init();
            totalCleanedLbl1.Text = "Total cleaned dirty:";
            totalCleanedLbl1.SetPosition(10, 60);
            totalCleanedLbl1.Width = 120;
            totalParamsGrpBox.Add(totalCleanedLbl1);

            Label totalCleanedLbl2 = new Label(Manager);
            totalCleanedLbl2.Init();
            totalCleanedLbl2.Text = "0";
            totalCleanedLbl2.SetPosition(totalCleanedLbl1.Left + totalCleanedLbl1.Width + 10, 60);
            totalParamsGrpBox.Add(totalCleanedLbl2);
            TotalStatsLbls.Add(totalCleanedLbl2);

            Label totalConsumedLbl1 = new Label(Manager);
            totalConsumedLbl1.Init();
            totalConsumedLbl1.Text = "Total consumed\nenergy:";
            totalConsumedLbl1.SetPosition(10, 85);
            totalConsumedLbl1.SetSize(120, 30);
            totalParamsGrpBox.Add(totalConsumedLbl1);

            Label totalConsumedLbl2 = new Label(Manager);
            totalConsumedLbl2.Init();
            totalConsumedLbl2.Text = "0";
            totalConsumedLbl2.SetPosition(totalConsumedLbl1.Left + totalConsumedLbl1.Width + 10, 85);
            totalParamsGrpBox.Add(totalConsumedLbl2);
            TotalStatsLbls.Add(totalConsumedLbl2);

            Label avarageDirtyLbl1 = new Label(Manager);
            avarageDirtyLbl1.Init();
            avarageDirtyLbl1.Text = "Average dirty\ndegree:";
            avarageDirtyLbl1.SetPosition(10, 115);
            avarageDirtyLbl1.SetSize(120, 30);
            totalParamsGrpBox.Add(avarageDirtyLbl1);

            Label avarageDirtyLbl2 = new Label(Manager);
            avarageDirtyLbl2.Init();
            avarageDirtyLbl2.Text = "0";
            avarageDirtyLbl2.SetPosition(avarageDirtyLbl1.Left + avarageDirtyLbl1.Width + 10, 115);
            totalParamsGrpBox.Add(avarageDirtyLbl2);
            TotalStatsLbls.Add(avarageDirtyLbl2);

            Label avarageCleanedLbl1 = new Label(Manager);
            avarageCleanedLbl1.Init();
            avarageCleanedLbl1.Text = "Average cleaned\ndirty:";
            avarageCleanedLbl1.SetPosition(10, 145);
            avarageCleanedLbl1.SetSize(120, 30);
            totalParamsGrpBox.Add(avarageCleanedLbl1);

            Label avarageCleanedLbl2 = new Label(Manager);
            avarageCleanedLbl2.Init();
            avarageCleanedLbl2.Text = "0";
            avarageCleanedLbl2.SetPosition(avarageCleanedLbl1.Left + avarageCleanedLbl1.Width + 10, 145);
            totalParamsGrpBox.Add(avarageCleanedLbl2);
            TotalStatsLbls.Add(avarageCleanedLbl2);

            Label avarageConsumedLbl1 = new Label(Manager);
            avarageConsumedLbl1.Init();
            avarageConsumedLbl1.Text = "Average consumed\nenergy:";
            avarageConsumedLbl1.SetPosition(10, 175);
            avarageConsumedLbl1.Width = 120;
            avarageConsumedLbl1.Height = 30;
            totalParamsGrpBox.Add(avarageConsumedLbl1);

            Label avarageConsumedLbl2 = new Label(Manager);
            avarageConsumedLbl2.Init();
            avarageConsumedLbl2.Text = "0";
            avarageConsumedLbl2.SetPosition(avarageConsumedLbl1.Left + avarageConsumedLbl1.Width + 10, 175);
            totalParamsGrpBox.Add(avarageConsumedLbl2);
            TotalStatsLbls.Add(avarageConsumedLbl2);
            /////
            //Select map

            OpenFileDialog = new Dialog(Manager);
            OpenFileDialog.Init();
            OpenFileDialog.Text = "Select map file";
            OpenFileDialog.Description.Visible = false;
            OpenFileDialog.Caption.Text = "";
            OpenFileDialog.Width = 600;
            OpenFileDialog.Resize += new ResizeEventHandler(OpenFileDialog_Resize);
            Manager.Add(OpenFileDialog);
            OpenFileDialog.Hide();

            FilesList = new ListBox(Manager);
            FilesList.Init();
            FilesList.Tag = System.Environment.CurrentDirectory;
            FilesList.SetPosition(0, 40);
            FilesList.ClientWidth = OpenFileDialog.Width - 15;
            FilesList.ClientHeight = OpenFileDialog.ClientHeight - 40;
            FilesList.TextColor = Color.Wheat;
            FilesList.DoubleClick += new EventHandler(FilesList_DoubleClick);
            GetFilesAndFolders();
            OpenFileDialog.Add(FilesList);

            LifeTimeLbl = new Label(Manager);
            LifeTimeLbl.Init();
            LifeTimeLbl.Text = "Life Time:";
            LifeTimeLbl.SetPosition(5, 10);
            OpenFileDialog.Add(LifeTimeLbl);

            LifeTimeTxt = new TextBox(Manager);
            LifeTimeTxt.Init();
            LifeTimeTxt.Text = "2000";
            LifeTimeTxt.SetSize(100, 25);
            LifeTimeTxt.SetPosition(5 + stepsLbl.Width, 5);
            LifeTimeTxt.TextColor = Color.WhiteSmoke;
            LifeTimeTxt.TextChanged += new EventHandler(TimeTxt_TextChanged);
            OpenFileDialog.Add(LifeTimeTxt);

            TestCaseLbl = new Label(Manager);
            TestCaseLbl.Init();
            TestCaseLbl.Text = "Test Case:";
            TestCaseLbl.SetPosition(LifeTimeTxt.Left + LifeTimeTxt.Width + 5, 10);
            OpenFileDialog.Add(TestCaseLbl);

            TestCaseTxt = new TextBox(Manager);
            TestCaseTxt.Init();
            TestCaseTxt.Text = "10";
            TestCaseTxt.SetSize(100, 25);
            TestCaseTxt.SetPosition(TestCaseLbl.Left + TestCaseLbl.Width + 5, 5);
            TestCaseTxt.TextColor = Color.WhiteSmoke;
            TestCaseTxt.TextChanged += new EventHandler(TimeTxt_TextChanged);
            OpenFileDialog.Add(TestCaseTxt);

            AgentLbl = new Label(Manager);
            AgentLbl.Init();
            AgentLbl.Text = "Agent:";
            AgentLbl.SetPosition(TestCaseTxt.Left + TestCaseTxt.Width + 5, 10);
            OpenFileDialog.Add(AgentLbl);

            AgentsComboBox = new ComboBox(Manager);
            AgentsComboBox.Init();
            AgentsComboBox.SetPosition(AgentLbl.Left + AgentLbl.Width - 10, 5);
            AgentsComboBox.Width = 150;
            AgentsComboBox.Height = 25;
            AgentsComboBox.TextColor = Color.Wheat;
            AgentsComboBox.Items.Add("RandomAgent");
            AgentsComboBox.Items.Add("ModelAgent");
            AgentsComboBox.Items.Add("ModelAgentNoIdle");
            AgentsComboBox.ItemIndex = 0;
            OpenFileDialog.Add(AgentsComboBox);

            NewGameDialog = new Dialog(Manager);
            NewGameDialog.Init();
            NewGameDialog.Text = "Start new tests";
            NewGameDialog.Description.Visible = false;
            NewGameDialog.BottomPanel.Visible = false;
            NewGameDialog.Caption.Text = "";
            NewGameDialog.Width = 600;
            NewGameDialog.Height = 130;
            NewGameDialog.TopPanel.Height = 100;
            Manager.Add(NewGameDialog);
            NewGameDialog.Hide();

            Button newGameOk = new Button(Manager);
            newGameOk.Init();
            newGameOk.SetPosition(20, 50);
            newGameOk.Text = "Confirm";
            newGameOk.Click += new EventHandler(newGameOk_Click);
            NewGameDialog.Add(newGameOk);

            EnableDisableMapControls(false);
        }

        private void ReparentControls(Control newParent)
        {
            TestCaseLbl.Parent = newParent;
            LifeTimeLbl.Parent = newParent;
            TestCaseTxt.Parent = newParent;
            LifeTimeTxt.Parent = newParent;
            AgentsComboBox.Parent = newParent;
        }

        /// <summary>
        /// Go to next run if we have runs left
        /// </summary>
        void NextRun()
        {
            if (CurrentRun < TestCase)
            {
                ++CurrentRun;
                DoOneStepBtn.Enabled = true;
                DisplayBtn.Enabled = true;
                DoOneRunBtn.Enabled = true;
                NextRunBtn.Enabled = false;

                StartLifecycle();
            }
        }

        /// <summary>
        /// Do all runs if we have runs left
        /// </summary>
        void DoAllRun()
        {
            DoOneRun(false);
            while ((CurrentRun + 1) < TestCase)
            {
                NextRun();
                DoOneRun(false);
            }
        }

        void DoOneRunParallel()
        {
            SelectMapBtn.Enabled = false;
            NewMapBtn.Enabled = false;
            EnableDisableMapControls(false);
            while (TotalStepsDone < LifeTime)
                DoOneStep();

            if ((CurrentRun + 1) == TestCase)
            {
                DoAllRunBtn.Enabled = false;
                NextRunBtn.Enabled = false;
            }
            EnableDisableMapControls(true);
            SelectMapBtn.Enabled = true;
            NewMapBtn.Enabled = true;
            MapControl.Invalidate();
        }

        /// <summary>
        /// Wrapper for DoOneRunParallel that can parallelize execution of this method
        /// </summary>
        /// <param name="parallel"></param>
        void DoOneRun(bool parallel)
        {
            if (parallel)
            {
                Thread thread = new Thread(DoOneRunParallel);
                thread.Start();
            }
            else
                DoOneRunParallel();
        }

        /// <summary>
        /// Reinitialize evaluator, agent, environment and statistics (used when go to next run)
        /// </summary>
        void StartLifecycle()
        {
            Environment = new Environment(SelectedMap);
            RNG = new RandomNumGen(Environment.RandomSeed + CurrentRun);
            Evaluator = new Evaluator();
            switch (AgentName)
            {
                case "RandomAgent":
                    CurrentAgent = new RandomAgent();
                    break;
                case "ModelAgent":
                    CurrentAgent = new ModelAgent();
                    break;
                case "ModelAgentNoIdle":
                    CurrentAgent = new ModelAgentNoIdle();
                    break;
            }

            TotalStepsDone = 0;
            StepsDone = 0;
            OverallDirty = 0;
            ConsumedEnergy = 0;
            DirtyOnMap = 0;
            CleanedDirty = 0;
            Displaying = false;
            RunStatsLbls[(int)RunStatistics.Action].Text = "";
            RunStatsLbls[(int)RunStatistics.TimeStep].Text = TotalStepsDone.ToString();

            DoOneStepBtn.Enabled = true;
            DoOneRunBtn.Enabled = true;
            DoAllRunBtn.Enabled = true;
            NextRunBtn.Enabled = false;
            GrpBox.Enabled = true;
            
            MapControl.Environment = Environment;
        }

        /// <summary>
        /// Gets files and folders in current folder and adds then to corresponding cotrol list
        /// </summary>
        private void GetFilesAndFolders()
        {
            FilesList.Items.Clear();
            FilesList.Items.Add(UP_FOLDER_NOTATION);
            string[] fd = Directory.GetDirectories((string)FilesList.Tag);
            int i = 0;
            for (; i < fd.Length; i++)
                fd[i] = Path.GetFileName(fd[i]) + FOLDER_NOTATION;
            FilesList.Items.AddRange(fd);
            fd = Directory.GetFiles((string)FilesList.Tag);
            for (i = 0; i < fd.Length; i++)
                if (Path.GetExtension(fd[i]) == MAP_EXTENSION)
                    FilesList.Items.Add(Path.GetFileName(fd[i]));
        }

        /// <summary>
        /// Enables/ disables controls depending on steps/runs left
        /// </summary>
        /// <param name="enabled"></param>
        private void EnableDisableMapControls(bool enabled)
        {
            if (enabled)
            {
                DoAllRunBtn.Enabled = TotalStepsDone < LifeTime;
                DoOneRunBtn.Enabled = TotalStepsDone < LifeTime;
                DoOneStepBtn.Enabled = TotalStepsDone < LifeTime;
                NextRunBtn.Enabled = (CurrentRun + 1) < TestCase && TotalStepsDone == LifeTime;
                GrpBox.Enabled = TotalStepsDone < LifeTime;
            }
            else
            {
                DoAllRunBtn.Enabled = enabled;
                DoOneRunBtn.Enabled = enabled;
                DoOneStepBtn.Enabled = enabled;
                NextRunBtn.Enabled = enabled;
                GrpBox.Enabled = enabled;
            }
        }

        private void DoOneStep()
        {
            if (Environment == null || TotalStepsDone >= LifeTime || 
                CurrentAgent == null)
                return;

            Environment.Change(RNG);

            CurrentAgent.Perceive(Environment);
            ActionType action = CurrentAgent.Think();

            Environment.AcceptAction(action);
            Evaluator.Eval(action, Environment);

            OverallDirty = Evaluator.TotalDirtyDegree;
            ConsumedEnergy = Evaluator.ConsumedEnergy;
            DirtyOnMap = Evaluator.DirtyDegree;
            CleanedDirty = Evaluator.CleanedDirty;

            ++TotalStepsDone;
            RunStatsLbls[(int)RunStatistics.Action].Text = action.ToString();
            RunStatsLbls[(int)RunStatistics.TimeStep].Text = TotalStepsDone.ToString();
            FinilizeLife();
        }

        /// <summary>
        /// In the end of run, upgrade total statistics and enable/disable UI controls
        /// </summary>
        void FinilizeLife()
        {
            if (TotalStepsDone == LifeTime)
            {
                DoOneStepBtn.Enabled = false;
                GrpBox.Enabled = false;
                DoOneRunBtn.Enabled = false;

                TotalDirtyDegree += Evaluator.TotalDirtyDegree;
                TotalConsumedEnergy += Evaluator.ConsumedEnergy;
                TotalCleanedDirty += Evaluator.CleanedDirty;

                if ((CurrentRun + 1) < TestCase)
                {
                    NextRunBtn.Enabled = true;
                }
                else
                    DoAllRunBtn.Enabled = false;
            }
        }

        /// <summary>
        /// Do some steps
        /// </summary>
        /// <param name="count">number of steps to do</param>
        private void DoSteps(object count)
        {
            for (int i = 0; i < (int)count; ++i)
            {
                MapControl.PosLerp = 0f;
                DoOneStep();
                ++StepsDone;

                if (StepsDone >= DisplaySteps || TotalStepsDone >= LifeTime)
                {
                    Displaying = false;
                    EnableDisableMapControls(true);
                    //TODO: any finish ops
                    break;
                }
            }
        }

        /// <summary>
        /// Update of the whole window
        /// </summary>
        /// <param name="gameTime">time since last update</param>
        protected override void Update(GameTime gameTime)
        {
            //if Display button was hit and we still need to display some of the requested steps
            if (Environment != null && Displaying)
            {
                int doSteps;
                if (DisplayTime > 0)
                {
                    TimeSinceStep += gameTime.ElapsedGameTime.TotalMilliseconds;
                    MapControl.PosLerp = (float)TimeSinceStep / DisplayTime;
                    doSteps = (int)(TimeSinceStep / DisplayTime);
                    TimeSinceStep -= doSteps * DisplayTime;
                    DoSteps(doSteps);
                }
                //If we don't need to display this steps then run in parallel to avoid UI sleeping
                else
                {
                    Thread thread = new Thread(new ParameterizedThreadStart(DoSteps));
                    thread.Start(DisplaySteps);
                }
            }
            MapControl.Invalidate();
            base.Update(gameTime);
        }

        #endregion

        #region Events

        void OpenFileDialog_Resize(object sender, ResizeEventArgs e)
        {
            FilesList.ClientWidth = OpenFileDialog.Width - 15;
            FilesList.ClientHeight = OpenFileDialog.ClientHeight - 40;
        }

        void selectMapBtn_Click(object sender, EventArgs e)
        {
            ReparentControls(OpenFileDialog);
            OpenFileDialog.ShowModal();
        }

        void exitBtn_Click(object sender, EventArgs e)
        {
            ExitDlg.ShowModal();
        }

        void DoOneRunBtn_Click(object sender, EventArgs e)
        {
            DoOneRun(true);
        }

        void TimeTxt_TextChanged(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            if (txtBox.Tag != null && (bool)txtBox.Tag)
            {
                txtBox.Tag = false;
                return;
            }
            if (txtBox == null || string.IsNullOrWhiteSpace(txtBox.Text))
                return;
            Match mtch = Rgx.Match(txtBox.Text);
            txtBox.Tag = true;
            if (mtch != null)
                txtBox.Text = mtch.Value;
        }

        void makeFullScreenBtn_Click(object sender, EventArgs e)
        {
            if (Manager.Graphics.IsFullScreen)
            {
                Manager.Graphics.PreferredBackBufferWidth = VacuumCleanerMain.WINDOW_WIDTH;
                Manager.Graphics.PreferredBackBufferHeight = VacuumCleanerMain.WINDOW_HEIGHT;
            }
            else
            {
                if (Manager.Graphics.GraphicsProfile == GraphicsProfile.Reach)
                {
                    Manager.Graphics.PreferredBackBufferWidth = System.Math.Min(Manager.GraphicsDevice.Adapter.CurrentDisplayMode.Width, 2048);
                    Manager.Graphics.PreferredBackBufferHeight = System.Math.Min(Manager.GraphicsDevice.Adapter.CurrentDisplayMode.Height, 2048);
                }
                else
                {
                    Manager.Graphics.PreferredBackBufferWidth = Manager.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
                    Manager.Graphics.PreferredBackBufferHeight = Manager.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
                }
            }

            Manager.Graphics.IsFullScreen = !Manager.Graphics.IsFullScreen;
            Manager.Graphics.ApplyChanges();
            ((Environment.TileMap3D)Renderers[1]).AdjustAspectRatio(Width, Height);
        }

        void displayBtn_Click(object sender, EventArgs e)
        {
            if (Environment == null)
                return;

            int temp;
            int.TryParse(TimeTxt.Text, out temp);
            DisplayTime = System.Math.Max(temp, 0);

            int.TryParse(StepsTxt.Text, out temp);
            DisplaySteps = System.Math.Max(1, temp);

            TimeSinceStep = 0.0f;
            StepsDone = 0;
            Displaying = true;
            EnableDisableMapControls(false);
        }

        void ExitDlg_Closed(object sender, WindowClosedEventArgs e)
        {
            if (ExitDlg.ModalResult == ModalResult.Yes)
                ThisApp.Exit();
        }

        void FilesList_DoubleClick(object sender, EventArgs e)
        {
            ListBox lstBox = (ListBox)sender;
            int idx = lstBox.ItemIndex;
            if (idx == -1 || idx >= lstBox.Items.Count)
                return;
            string item = (string)lstBox.Items[idx];
            string currentDir = (string)lstBox.Tag;
            if (item == UP_FOLDER_NOTATION)
                lstBox.Tag = currentDir.Substring(0, currentDir.LastIndexOfAny(new char[] { '/', '\\' }));
            else
                if (item.Contains(FOLDER_NOTATION))
                    lstBox.Tag = (string)lstBox.Tag + '/' + item.Replace(FOLDER_NOTATION, "");
                else
                    SelectedMap = (string)lstBox.Tag + '/' + item;

            if (SelectedMap == null)
                GetFilesAndFolders();
            //if new map was selected
            else
            {
                int test_case, life_time;
                int.TryParse(TestCaseTxt.Text, out test_case);
                TestCase = System.Math.Max(test_case, 1);
                int.TryParse(LifeTimeTxt.Text, out life_time);
                if (life_time <= 0)
                    life_time = 2000;
                LifeTime = life_time;

                CurrentRun = 0;
                TotalCleanedDirty = 0;
                TotalConsumedEnergy = 0;
                TotalDirtyDegree = 0;

                EnvironmentChanged = true;
                AgentName = AgentsComboBox.Items[AgentsComboBox.ItemIndex].ToString();

                StartLifecycle();
                OpenFileDialog.Close();
            }
        }

        void DoAllRunBtn_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(DoAllRun);
            thread.Start();

            DoAllRunBtn.Enabled = false;
        }

        void DoOneStepBtn_Click(object sender, EventArgs e)
        {
            DoOneStep();
        }

        /// <summary>
        /// Selecting the renderer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RenderersComboBox_TextChanged(object sender, EventArgs e)
        {
            switch(RenderersComboBox.Text)
            {
                case "TileMap2D":
                    MapControl.Movable = true;
                    MapControl.Resizable = true;
                    if (MapControl.Tag != null)
                    {
                        Point pt = (Point)MapControl.Tag;
                        MapControl.ClientWidth = pt.X;
                        MapControl.ClientHeight = pt.Y;
                    }
                    MapControl.Renderer = Renderers[0];
                    break;
                case "TileMap3D":
                    MapControl.Tag = new Point(MapControl.ClientWidth, MapControl.ClientHeight);
                    MapControl.Movable = false;
                    MapControl.Resizable = false;
                    MapControl.SetPosition(ClientLeft, ClientTop);
                    MapControl.ClientWidth = ClientWidth;
                    MapControl.ClientHeight = ClientHeight;
                    MapControl.Renderer = Renderers[1];
                    ((Environment.TileMap3D)Renderers[1]).AdjustAspectRatio(Width, Height);
                    break;
            }
        }

        void NextRunBtn_Click(object sender, EventArgs e)
        {
            NextRun();
        }

        protected RenderTarget2D CreateRenderTarget(int width, int height)
        {
            return new RenderTarget2D(Manager.GraphicsDevice, Manager.Graphics.PreferredBackBufferWidth, Manager.Graphics.PreferredBackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents);
        }

        void NewMapBtn_Click(object sender, EventArgs e)
        {
            if (SelectedMap == null)
                return;
            ReparentControls(NewGameDialog);
            NewGameDialog.ShowModal();
        }

        void newGameOk_Click(object sender, EventArgs e)
        {
            int test_case, life_time;
            int.TryParse(TestCaseTxt.Text, out test_case);
            TestCase = System.Math.Max(test_case, 1);
            int.TryParse(LifeTimeTxt.Text, out life_time);
            if (life_time <= 0)
                life_time = 2000;
            LifeTime = life_time;

            CurrentRun = 0;
            TotalCleanedDirty = 0;
            TotalConsumedEnergy = 0;
            TotalDirtyDegree = 0;

            EnvironmentChanged = true;
            AgentName = AgentsComboBox.Items[AgentsComboBox.ItemIndex].ToString();

            StartLifecycle();
            NewGameDialog.Close();
        }

        #endregion
    }
}
