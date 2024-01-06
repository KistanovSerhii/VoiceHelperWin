using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.Synthesis;
using System.Globalization;
using System.Xml;

using SpeechLib;

namespace Voice_helper
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;

        public Form1()
        {
            InitializeComponent();
        }

        public static string userName;
        public static XmlDocument settingsXML = new XmlDocument();
        public static ProgramSetting[] programSettingsArr;
        public static List<UserInitiatedProcesses> runningUserProcessesList;
        public static bool listen;
        public static ListBox lbox1;
        public static string commandNameListenTurnON;
        public static string commandNameListenTurnOFF;
        public static float recognitionAccuracy;

        public static int selectedVoiceIndex;
        public static int selectedVoiceRate;

        private void Form1_Shown(object sender, EventArgs e)
        {
            runningUserProcessesList = new List<UserInitiatedProcesses>();
            lbox1 = listBox1;
            listBoxShowUserMessage("Инициализация настроек программы ...");

            if (!loadDataSettingsXML()) return; // ошибка загрузки из settings.xml

            string minimized = GetXmlNodeValue("/Settings/minimized[1]/text()");
            if (minimized != "" && minimized != "0" && minimized.ToLower() != "false" && minimized.ToLower() != "ложь") 
            {
                this.WindowState = FormWindowState.Minimized;
            }

            ShowAvailableRussianVoices();

            selectedVoiceIndex = -1;
            string selectedVoiceName = GetXmlNodeValue("/Settings/assistantVoiceName[1]/text()");
            if (selectedVoiceName != "") {
                selectedVoiceIndex = FindVoiceIndex(selectedVoiceName);
            }
            
            if (selectedVoiceIndex != -1) listBoxShowUserMessage($"Текущий голос помощника: {selectedVoiceName}");

            selectedVoiceRate = 1;
            string userVoiceRate = GetXmlNodeValue("/Settings/assistantVoiceRate[1]/text()");
            if (userVoiceRate != "")
            {
                // Громкость Голоса от 0 до 100
                selectedVoiceRate = int.Parse(userVoiceRate);
                selectedVoiceRate = selectedVoiceRate > 100 ? 100 : selectedVoiceRate;
                selectedVoiceRate = selectedVoiceRate < 0 ? 0 : selectedVoiceRate;
            }
            listBoxShowUserMessage($"Текущая громкость помощника: {selectedVoiceRate}");

            string defUserName = GetXmlNodeValue("/Settings/defaultUserNameUse[1]/text()");
            if (defUserName != "")
            {
                userName = defUserName;
            } else {
                // Реализовать возможность пользователю установить имя интерактивно { а пока прекращаем выполнение }
                listBoxShowUserMessage("Имя пользователя по умолчанию не установлено!");
                listBoxShowUserMessage("Заполните тег <defaultUserNameUse> в файле settings.xml и перезапустите приложение!");
                return;
            }

            if (!initializationOfProgramSettingsByUser(userName)) return; // ошибка инициализации команд пользователя userName из settings.xml

            initializationSpeech();
        }

        static string GetXmlNodeValue(String nodePath, int itemIndex = 0)
        {
            var selectNode = settingsXML.SelectNodes(nodePath);
            string nodeValue = selectNode.Count > 0 ? settingsXML.SelectNodes(nodePath).Item(itemIndex).Value.ToString() : "";
            return nodeValue;
        }

        static void ShowAvailableRussianVoices()
        {
            listBoxShowUserMessage("Доступные голоса помощника:");
            SpVoice voice = new SpVoice();

            foreach (ISpeechObjectToken voiceToken in voice.GetVoices(string.Empty, string.Empty))
            {
                String currentVoiceName = voiceToken.GetDescription();

                // Проверяем, содержит ли currentVoiceName строку "ru" (без учета регистра)
                if (currentVoiceName.IndexOf("ru", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    listBoxShowUserMessage($"{currentVoiceName}");
                }
            }
        }

        static int FindVoiceIndex(string voiceName)
        {
            int voiceIndex = 0;
            SpVoice voice = new SpVoice();

            foreach (ISpeechObjectToken voiceToken in voice.GetVoices(string.Empty, string.Empty))
            {
                String currentVoiceName = voiceToken.GetDescription();

                // Сравниваем без учета регистра и пробелов
                if (currentVoiceName.Replace(" ", "").Equals(voiceName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                {
                    return voiceIndex;
                }

                voiceIndex += 1;
            }

            // Если не найдено совпадение
            return -1;
        }

        static bool initializationOfProgramSettingsByUser(string userName)
        {
            // Список разрещенных/обрабатываемых команд относящихся к системным
            string[] availableSystemCommandsArr = new String[] {
                "ListenTurnON",
                "ListenTurnOFF",
                "killCurrentProcess",
                "killAllProcess",
                "SendKeys",
                "Exit",
                "shutdownPC"
            };
            
            // initialization program settings +++

            string aliasName    = "";
            string argument     = "";
            string actionValue  = "";
            string actionName   = "";

            // Установка знч точности распознавания +++
            recognitionAccuracy = 0.8f; // Точность распознавания по умолчанию!
            try
            {
                string recognitionAccuracyValue =
                    settingsXML.SelectSingleNode($"//User[@name='{userName}']/RecognitionAccuracy")?.InnerText;
                if (float.TryParse(recognitionAccuracyValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                {
                    recognitionAccuracy = result;
                }
            }
            catch (Exception ex)
            {
                listBoxShowUserMessage("Ошибка получения RecognitionAccuracy, подробно: " + ex.ToString());
            }
            // Установка знч точности распознавания ---

            var programSettingsNodes = settingsXML.SelectNodes(
                "//User[@name='" + userName + "']/ExequteTypes/ProgramSettings/Actions/action");
            programSettingsArr = new ProgramSetting[programSettingsNodes.Count];


            for (int i = 0; i < programSettingsNodes.Count; i++)
            {
                aliasName = programSettingsNodes[i].Attributes[0].InnerText.ToString();
                if (programSettingsNodes[i].Attributes.Count > 1)
                {
                    argument = programSettingsNodes[i].Attributes[1].Value.ToString();
                }
                else
                {
                    argument = "";
                }
                actionValue         = programSettingsNodes[i].InnerText.ToString();
                actionName          = programSettingsNodes[i].Attributes.GetNamedItem("aliasName").InnerText.ToString();

                //recognitionAccuracy = float.Parse(rAccuracy.InnerText.ToString());

                if ( !findValueInArr(availableSystemCommandsArr, actionName) ) continue;

                if (aliasName.ToUpper() == "ListenTurnON".ToUpper()) commandNameListenTurnON = actionValue;
                if (aliasName.ToUpper() == "ListenTurnOFF".ToUpper()) commandNameListenTurnOFF = actionValue;

                programSettingsArr[i] = new ProgramSetting(aliasName, argument, actionValue);
                listBoxShowUserMessage("program settings Add (key:" + aliasName + " | value:" + actionValue + ") ...");
            }
            
            // initialization program settings ---
            
            return true;
        }

        static void initializationSpeech()
        {
            listen = false;

            if (commandNameListenTurnON == null)
            {
                listBoxShowUserMessage("Ошибка получения команды активации \"ListenTurnON\" в файле settings.xml!");
                return;
            }
            if (commandNameListenTurnOFF == null)
            {
                listBoxShowUserMessage("Ошибка получения команды тишины \"ListenTurnOFF\" в файле settings.xml!");
                return;
            }


            try
            {
                CultureInfo ci = new CultureInfo("ru-ru");
                SpeechRecognitionEngine sre = new SpeechRecognitionEngine(ci);
                sre.SetInputToDefaultAudioDevice();
                sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sre_SpeechRecognized);
                
                // initialization base settings (системные и пользовательские action такие как открой, выполни и т.д.) +++
                Choices actionCommandsNames = new Choices();
                var action = settingsXML.SelectNodes("//User[@name='" + userName + "']/ExequteTypes//action");

                // doubleCheck - "Сито", для удаления повторяющихся команд (оставим только уникальные)
                string[] doubleCheck = new String[action.Count];
                for (var i = 0; i < action.Count; i++)
                {
                    if (findValueInArr(doubleCheck, action[i].InnerText)) continue;
                    doubleCheck[i] = action[i].InnerText;
                    actionCommandsNames.Add(new string[] { action[i].InnerText });
                    listBoxShowUserMessage("Action commands names Add (" + action[i].InnerText + ") ...");
                }
                // initialization base settings (системные и пользовательские action такие как открой, выполни и т.д.) ---

                // initialization user settings (alias конкретного действия такие как Excel, 1С, RDP и т.д.) +++
                /*      
                 *  Три Choices - это ключ команды из трех слов
                 *  одно слово из набора actionCommandsNames
                 *  одно слово из набора aliasCommandsNames
                 *  Так как есть команды состоящие из
                 *  двух слов (пример: ДжейДжей Привет)
                 *  в третем Choices необходим пустой вариант ""
                 */
                Choices aliasCommandsNames = new Choices();
                aliasCommandsNames.Add(new string[] { " " });
                var alias = settingsXML.SelectNodes("//User[@name='" + userName + "']/ExequteTypes//alias");
                doubleCheck = new String[alias.Count];
                for (var i = 0; i < alias.Count; i++)
                {
                    if (findValueInArr(doubleCheck, alias[i].InnerText)) continue;
                    doubleCheck[i] = alias[i].InnerText;
                    aliasCommandsNames.Add(new string[] { alias[i].InnerText });
                    listBoxShowUserMessage("Alias commands name Add (" + alias[i].InnerText + ") ...");
                }
                doubleCheck = null;
                // initialization user settings (alias конкретного действия такие как Excel, 1С, RDP и т.д.) ---

                GrammarBuilder gb = new GrammarBuilder();
                gb.Culture = ci;
                gb.Append(actionCommandsNames);
                gb.Append(aliasCommandsNames);

                Grammar g = new Grammar(gb);
                sre.LoadGrammar(g);
                sre.RecognizeAsync(RecognizeMode.Multiple);

                listBoxShowUserMessage("Компоненты успешно инициализированы!");
                listBoxShowUserMessage("Привет " + userName  + "!");
                listBoxShowUserMessage("Для активации, скажи " + commandNameListenTurnON);
                
                // saySomeText("Привет " + userName  + "!", false);
            }
            catch (Exception ex)
            {
                listBoxShowUserMessage("Ошибка инициализации необходимых компонент. подробно: \n\n ex.ToString()" +
                    "\n\n Убедитесь что ваш микрофон включе и работает исправно, \n " +
                    " если это не так - включите его и перезапустите приложение! <--- " + ex.Message);
            }
        }

        static void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {

            if (e.Result.Confidence < recognitionAccuracy) return;

            if (e.Result.Text.ToUpper() == commandNameListenTurnON.ToUpper())
            {
                listen = true;
                listBoxShowUserMessage("Вы:" + e.Result.Text);
                saySomeText("Слушаю!");
                return;
            }
            else if (e.Result.Text.ToUpper() == commandNameListenTurnOFF.ToUpper())
            {
                listen = false;
                listBoxShowUserMessage("Вы:" + e.Result.Text);
                saySomeText("Молчу!");
                return;
            }

            if (!listen) return;

            listBoxShowUserMessage("Ваша команда: " + e.Result.Text);
            try
            {
                /* ******************************************************************************* */
                // ОБЯЗАТЕЛЬНО ПРОВЕРИТЬ aliasAtrNameArr.Count > 0;
                // find exequte command {
                // Проверка на команду из раздела "ProgramSettings" {
                /*
                 *  !!! Все команды должны соблюдать верблюжий стиль, Пример: МояКомандаОдин
                */
                string actionComand = e.Result.Words[0].Text.ToString();
                string aliasComand  = e.Result.Words.Count > 1 ? e.Result.Words[1].Text.ToString() : "";
                ProgramSetting progSett = findCommandInProgramSettingsArr(actionComand);
                if (progSett != null)
                {
                    // Зарезервированная системная команда
                    switch (progSett.aliasName)
                    {
                        case "SendKeys":
                            SendKeys.SendWait(progSett.argument);
                            break;
                        case "killCurrentProcess":
                            stopProcess(aliasComand, false);
                            break;
                        case "killAllProcess":
                            stopProcess(aliasComand, true);
                            break;
                        case "Exit":
                            Process.GetCurrentProcess().Close();
                            Process.GetCurrentProcess().Kill();
                            break;
                    }
                    listBoxShowUserMessage("Выполнение зарезервированной команды " + progSett.aliasName + "/" + actionComand); // Путь исполняемого файла
                    return;
                }
                var aliasAtrNameArr = settingsXML.SelectNodes("//User[@name='" + userName + "']/ExequteTypes//alias[text()='" + aliasComand + "']/@name");
                var typeNodeNameArr = aliasAtrNameArr.Item(0).SelectNodes("../../../node()"); // Подняться к родителю
                string typeNodeName = typeNodeNameArr.Item(0).ParentNode.Name.ToString(); // type-nodeName
                string aliasAtrName = aliasAtrNameArr.Item(0).Value.ToString(); // alias-atrName
                var c1 = settingsXML.SelectNodes("//ExequteCommands/command[@type='" + typeNodeName + "' and text()='" + aliasAtrName + "']");
                for (var i = 0; i < c1.Count; i++)
                {
                    if (c1.Item(i).Attributes[0].Name.ToUpper() != "type".ToUpper()
                        || c1.Item(i).Attributes[1].Name.ToUpper() != "fullPath".ToUpper()
                        || c1.Item(i).Attributes[2].Name.ToUpper() != "program".ToUpper()
                        || c1.Item(i).Attributes[3].Name.ToUpper() != "argument".ToUpper()) return;

                    bool explorerProcess = c1.Item(i).Attributes[0].Value.ToUpper() == "ExplorerAlias".ToUpper();

                    startProcess(aliasComand, c1.Item(i).Attributes[1].Value.ToString() 
                        + c1.Item(i).Attributes[2].Value.ToString(), c1.Item(i).Attributes[3].Value.ToString(), explorerProcess);
                }
            }
            catch (Exception ex)
            {
                listBoxShowUserMessage("Ошибка обработки команды, подробно: " + ex.ToString());
            }
        }
        //
        static void startProcess(string userCommand, string fileName, string existArg = "", bool waitForExit = false)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = fileName;
            processInfo.Arguments = existArg;
            processInfo.Verb = "runas";
            processInfo.UseShellExecute = false;
            string processName = "explorer";
            try
            {
                /* 
                 *  Если открывается папка тогда - 
                 *  обязательно ожидать завершения,
                 *  иначе не получиться выполнить Kill()!
                */
                if (waitForExit)
                {
                    Process.Start(processInfo).WaitForExit();
                    listBoxShowUserMessage("Wait for exit " + fileName);
                }
                else
                {
                    processName = Process.Start(processInfo).ProcessName;
                    listBoxShowUserMessage("Выполнен запуск " + "@" + fileName);
                }
                DateTime startTime = DateTime.Now;
                List<UserInitiatedProcesses> lastSession = runningUserProcessesList.FindAll(x => x.commandName.Contains(userCommand));
                int sessionId = lastSession.Count > 0 ? lastSession.Last().sessionId : 1;
                runningUserProcessesList.Add(new UserInitiatedProcesses(/*sessionId++*/1, userCommand, processName, startTime));
            }
            catch (Exception err)
            {
                listBoxShowUserMessage("Ошибка попытки запуска процесса, Error: " + err.Message);
            }
        }

        public static void stopProcess(string userCommand, bool stopAllProcess = false)
        {
            int killCount = 0;
            List<UserInitiatedProcesses> userProcessCommandArr 
                = runningUserProcessesList.FindAll(x => x.commandName.ToUpper() == userCommand.ToUpper());
            
            if (stopAllProcess)
            {
                userProcessCommandArr = runningUserProcessesList;
            }
            if (userProcessCommandArr.Count() > 0)
            {
                UserInitiatedProcesses lastUserProcess = userProcessCommandArr.Last();
                for (var i = 0; i < userProcessCommandArr.Count; i++)
                {
                    Process[] currentProcessesArr = Process.GetProcessesByName(userProcessCommandArr[i].processName);
                    if (currentProcessesArr.Count() == 0) continue;
                    for (var j = 0; j < currentProcessesArr.Length; j++)
                    {
                        if (currentProcessesArr[j].StartTime.ToString().ToUpper() 
                            == lastUserProcess.startTime.ToString().ToUpper()
                            || stopAllProcess && currentProcessesArr[j].StartTime.ToString().ToUpper()
                            == userProcessCommandArr[i].startTime.ToString().ToUpper() )
                        {
                            killProcess(currentProcessesArr[j]);
                            killCount++;
                        }
                    }
                }
            }

            // Когда killAllProcess Тогда userCommand не требуется (пустой), но Если killCurrentProcess Тогда
            // userCommand обязан быть заполнен! (так как userCommand это имя команды которую надо завершить)

            if (killCount == 0)
            {
                string currentMsg = userCommand == ""
                ? "Вы должны сказать имя команды которую хотите закрыть (например: Хватит Блокнот)"
                : "Нет процессов запущенных данным приложением и зарегистрированных под командой " + userCommand;

                listBoxShowUserMessage(currentMsg);
            }
            else
            {
                listBoxShowUserMessage("\n Процессов найдено и завершено " + killCount + " \n");
            };
        }

        static void killProcess(Process pRef)
        {
            try
            {
                pRef.Kill();
                listBoxShowUserMessage("Процесс имя:(" + pRef.ProcessName + ") id:(" + pRef.Id + ") - найден и завершен!");
            }
            catch (Exception err)
            {
                listBoxShowUserMessage("Ошибка попытки заверщить процесса id:" + pRef.Id + ", name:" + pRef.ProcessName + ". Error:" + err.Message);
            }
        }

        static bool loadDataSettingsXML()
        {
            try
            {
                settingsXML.Load("settings.xml");
            }
            catch (Exception ex)
            {
                listBoxShowUserMessage("Ошибка загрузки файла настроек (\"settings.xml\"), подробно: " + ex.Message);
                return false;
            }
            return true;
        }

        static ProgramSetting findCommandInProgramSettingsArr(string findCommand)
        {
            foreach (ProgramSetting s in programSettingsArr)
            {
                if (s.actionValue.ToUpper() == findCommand.ToUpper()) return s;
            }
            return null;
        }

        static bool findValueInArr(string[] arr, string find)
        {
          
            for (var i = 0; i < arr.Length; i++)
            {
                if (arr[i] == null) return false;

                if (arr[i].ToUpper() == find.ToUpper()) return true;
            }
            return false;
        }

        static void listBoxShowUserMessage(string showTxt)
        {
            lbox1.Items.Add(showTxt);
            lbox1.SelectedIndex = lbox1.Items.Count - 1;
        }

        // В системе обязательно должен быть установлен хотябы один голос
        // (например ivona voices Татьяна TTX)
        // Ссылка на голоса www.microsoft.com/en-us/download/details.aspx?id=27224
        static void saySomeText(string sayText, bool speakAsync = true, string sender = "", int rate = 1)
        {
            try
            {
               if (selectedVoiceIndex != -1) { 
                    SpVoice userSpeech = new SpVoice();
                    userSpeech.Voice = userSpeech.GetVoices(string.Empty, string.Empty).Item(selectedVoiceIndex);
                    userSpeech.Volume = 100; //Громкость Голоса 0 - 100
                    userSpeech.Rate = rate; //Скорость голоса 0 - 9

                    SpeechVoiceSpeakFlags speakFlag = speakAsync ? SpeechVoiceSpeakFlags.SVSFlagsAsync : SpeechVoiceSpeakFlags.SVSFDefault;

                    userSpeech.Speak(sayText, speakFlag);
                    return;
                }

                SpeechSynthesizer speech = new SpeechSynthesizer();
                
                var voiceList = speech.GetInstalledVoices();
                speech.SelectVoice(voiceList[0].VoiceInfo.Name);

                speech.SetOutputToDefaultAudioDevice();
                speech.Rate = rate;
                speech.Volume = 100;
                
                listBoxShowUserMessage(sender != "" ? "-" + sender + ":" + sayText : sayText);
                
                if (speakAsync)
                {
                    speech.SpeakAsync(sayText);
                }
                else
                {
                    speech.Speak(sayText);
                }
            }
            catch (Exception ex)
            {
                listBoxShowUserMessage("Ошибка воспроизведения текста, подробно: " + ex.ToString());
            }
        }
        private void listBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = listBox1.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    listBox1.SelectedIndex = index;

                    ContextMenu contextMenu = new ContextMenu();
                    MenuItem copyMenuItem = new MenuItem("Копировать");
                    copyMenuItem.Click += (copySender, copyEventArgs) =>
                    {
                        if (listBox1.SelectedIndex != -1)
                        {
                            Clipboard.SetText(listBox1.SelectedItem.ToString());
                        }
                    };

                    contextMenu.MenuItems.Add(copyMenuItem);

                    listBox1.ContextMenu = contextMenu;
                    contextMenu.Show(listBox1, e.Location);
                }
            }
        }

        // #Область "Приложение будет сворачиваться в трей при минимизации"
        private void Form1_Load(object sender, EventArgs e)
    {
        notifyIcon = new NotifyIcon
        {
            Icon = this.Icon,
            Text = this.Text,
            Visible = true
        };

        // Обработчик клика по иконке в трее
        notifyIcon.Click += (s, args) => ShowMainWindow();
    }

    private void Form1_Resize(object sender, EventArgs e)
    {
        // Если форма минимизирована, сворачиваем в трей
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
            notifyIcon.Visible = true;
        }
    }

    private void ShowMainWindow()
    {
        // Разворачиваем форму и показываем ее
        Show();
        WindowState = FormWindowState.Normal;
        notifyIcon.Visible = false;
    }

    // Ваши существующие обработчики событий...

    private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        // Обработчик двойного клика по иконке в трее
        ShowMainWindow();
    }

    // Ваши существующие методы...

    private void ExitApplication()
    {
        // Ваш код завершения приложения...
        Application.Exit();
    }
        // #КонецОбласти "Приложение будет сворачиваться в трей при минимизации"

    }
}
