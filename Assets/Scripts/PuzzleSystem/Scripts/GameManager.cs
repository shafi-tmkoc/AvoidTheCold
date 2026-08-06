//using System;
//using System.Collections;
//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//namespace AvoidTheCold
//{
//    [System.Serializable]
//    public enum GameState
//    {
//        FirstTimeStart, Start, Playing, Stop, GameComplete, LevelComplete, LevelFail
//    }
//    public class GameManager : Singleton<GameManager>
//    {
//        public bool InTestMode;
//        public ScreenOrientation orientation = ScreenOrientation.LandscapeRight;
//        public GameState gameState;
//        public int levelNo = 0;

//        public List<LevelGrid_SO> levels;

//        public TextMeshProUGUI timer;

//        GameCategoryDataManager _gameCategoryData;
//        UpdateCategoryApiManager _updateCategoryManager;
//        public int gameID;

//         public int time = 60;


//        public static Action OnGameStart, OnGamePlaying, OnGameStop, OnLevelCompleted, OnLevelFailed, OnGameCompleted;

//        protected override void Awake()
//        {
//            base.Awake();
//            Application.targetFrameRate = 60;
//            Screen.orientation = orientation;


//#if PLAYSCHOOL_MAIN
//            gameID =  PlayerPrefs.GetInt("currentGameId");
//#endif


//            //GameCategoryDataManager Paramert (GameId (form playerpref), Game Name (form playerpref))


//            _gameCategoryData = new GameCategoryDataManager(gameID, PlayerPrefs.GetString("currentGameName"));
//            _updateCategoryManager = new UpdateCategoryApiManager(gameID);


//            levelNo = _gameCategoryData.GetCompletedLevel; // Get Completed Level


//            if (levelNo >= levels.Count)
//            {
//                levelNo = 0;
//            }


//        }

//        void Start()
//        {
//            Application.targetFrameRate = 60;
//            FirstStart();
//            CheckIntro();

//            // InitGame();
//        }

//        public void CheckIntro()
//        {
//            if (levels[levelNo].newParent != null)
//            {
//                var ui = UIManager.Instance; 
//                ui.HandleObject(ui.introPanel, true);
//                ui.SetIntro(levels[levelNo].newParent, levels[levelNo].newChild, levels[levelNo].parentName, levels[levelNo].childName, levels[levelNo].introText);

//                // AudioManager.Instance.PlayAudio(AudioManager.Instance.voiceOverSource, AudioManager.Instance)
//                //UIManager.Instance.continueButton.interactable = false;
//                //AudioManager.Instance.PlayLevelIntro(levelNo, () => UIManager.Instance.continueButton.interactable = true);
//                if (RuntimeAudioLoader.Instance != null)
//                {
//                    AudioManager.Instance.PlayLevelIntro(levelNo, () =>
//                    {
//                        Debug.Log("Intro Completed");
//                        ui.HandleObject(ui.introPanel, false);
//                        InitGame();
//                        return;
//                    });
//                }

//                if (InTestMode)
//                {
//                    ui.HandleObject(ui.introPanel, false);
//                    InitGame();
//                }
//            }
//            else
//            {
//                InitGame();
//            }

//        }



//        Coroutine countDown_CO;


//        public void InitGame()
//        {
//            if (gameState == GameState.Stop) return;    
//            AudioManager.Instance.PlayLevelHint(levelNo);
//            UIManager.Instance.introPanel.SetActive(false);
//            UIManager.Instance.gamePanel.SetActive(true);
//            UIManager.Instance.SetOutroText(levels[levelNo].outroText);
//            if (countDown_CO != null) StopCoroutine(countDown_CO);
//            countDown_CO = StartCoroutine(CountDown_CO(time));

//            GameStarted();
//            GridGenerator.Instance.DeleteGrid();
//            GridGenerator.Instance.levelGrid = levels[levelNo];
//            GridGenerator.Instance.DrawGrid();
//            GamePlaying();

//           // UIManager.Instance.DOPop(UIManager.Instance.gamePanel.transform, Vector2.one * 0.5f, Vector2.one * 1.2f, 0.5f);
//        }

//        public void LoadNextLevel()
//        {
//            levelNo++;
//            // levelNo = levelNo > levels.Count - 1 ? 0 : levelNo;
//            if (levelNo > levels.Count - 1)
//            {
//                Debug.Log("All levels are completed");
//                EnableFinalWinPanel();
//                return;
//            }
//            CheckIntro();
//        }

//        public void FirstStart()
//        {
//            gameState = GameState.FirstTimeStart;
//            GameStarted();

//        }

//        public void GameStarted()
//        {
//            gameState = GameState.Start;
//            OnGameStart?.Invoke();
//        }

//        public void GameStopped()
//        {
//            gameState = GameState.Stop;
//            OnGameStop?.Invoke();
//        }

//        public void GamePlaying()
//        {
//            gameState = GameState.Playing;
//            OnGamePlaying?.Invoke();
//        }

//        public void GameCompleted()
//        {
//            gameState = GameState.GameComplete;
//            OnGameCompleted?.Invoke();
//        }

//        public void LevelCompleted()
//        {
//            AudioManager.Instance.PlayAudio(AudioManager.Instance.sfxSource, AudioManager.Instance.winSFX);
//            gameState = GameState.LevelComplete;
//            if (UIManager.Instance.nextButton != null) UIManager.Instance.nextButton.interactable = false;
//            if (UIManager.Instance.retryButton != null) UIManager.Instance.retryButton.interactable = false;


//            AudioManager.Instance.PlayLevelOutro(levelNo, () =>
//            {
//                if (UIManager.Instance.nextButton != null) UIManager.Instance.nextButton.interactable = true;
//                if (UIManager.Instance.retryButton != null) UIManager.Instance.retryButton.interactable = true;
//            });
//            OnLevelCompleted?.Invoke();
//        }

//        public void LevelFailed()
//        {
//            gameState = GameState.LevelFail;
//            // losePanel.SetActive(true);
//            OnLevelFailed?.Invoke();
//            //UIManager.Instance.DOPop(UIManager.Instance.losePanel.transform, Vector2.one * 0.5f, Vector2.one * 1.2f, 0.5f);
//        }

//        public void PlayWinSequence()
//        {
            
//            GameStopped();
//            if (countDown_CO != null) StopCoroutine(countDown_CO);
//            PlaySchool_SaveLevelData(levelNo + 1);
//            StartCoroutine(GridHandler.Instance.WaitUntil(() => GridHandler.Instance.AreAllAnimalsIdle(), () => LevelCompleted()));
//        }

//        public void ReloadCurrentLevel()
//        {
//            InitGame();
//        }

//        public void EnableFinalWinPanel()
//        {
//#if PLAYSCHOOL_MAIN
//            EffectParticleControll.Instance.SpawnGameEndPanel();
//            GameOverEndPanel.Instance.AddTheListnerRetryGame(() => LevelManager.Instance.LoadNextLevel);

//#else
//            //     //Your testing End panel
//            //physicscar.UIManager.Instance.SwitchMenus(tmkoc.games.physicscar.UIManager.Instance.EndScreenMenu.gameObject);
//#endif
//        }

//        public virtual void GoBackToPlayschool()
//        {
//            //  SceneManager.LoadScene("Scenes/MainScene");
//            //     UnityEngine.Debug.Log("Go back to playschool");

//            SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu);
//            //     #if PLAYSCHOOL_MAIN
//            // //     //dataManager.SendData(()=>
//            //      #else
//            //          UnityEngine.Debug.Log("Go back to playschool");
//            //      #endif
//        }

//        public void PlaySchool_SaveLevelData(int levelNum)
//        {
//            int num = levelNum;
//            //SaveLevel  Parameter:  ((save)currentLevel , totalLevel)
//            _gameCategoryData.SaveLevel(num, levels.Count);
//            // this _star the get load ed star from json
//            int _star = _gameCategoryData.GetLoadedstar;
//            // SetGameDataMore this for in main play school app for send the data to server 
//            //SetGameDataMore Parameter:  ((save)currentLevel , totalLevel , stars)
//            if (_star == 5)
//            {
//                //After One Time full game completed Logic
//                _updateCategoryManager.SetGameDataMore(num, levels.Count, 5);
//            }
//            else
//            {
//                //Playing Game for First Time
//                _updateCategoryManager.SetGameDataMore(num, levels.Count, _star);
//            }
//        }

//        IEnumerator CountDown_CO(int startFrom)
//        {
//            int count = startFrom;

//            // Initialize text immediately
//            timer.text = count.ToString("00") + ":" + count.ToString("00");

//            while (count >= 0)
//            {
//                //timer.text = "00:" + count.ToString("00");
//                timer.text = TimeSpan.FromSeconds(count).ToString(@"mm\:ss");
//                //timer.transform.DOScale(Vector2.one * 1.5f, 0.25f).SetLoops(2, LoopType.Yoyo);

//                count--;
//                yield return new WaitForSeconds(1);
//            }

//            if (gameState == GameState.Playing)
//            {
//                LevelFailed();
//            }
//            yield return null;
//        }

//    }
//}
