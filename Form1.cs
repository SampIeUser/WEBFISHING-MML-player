using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;


namespace SV_WEBFISHING_GuitarMIDI
{
    public partial class Form1 : Form
    {

        private List<Point> calibrationPoints; // Список для хранения точек калибровки
        private bool isCalibrating; // Флаг для определения состояния калибровки
        private LowLevelMouseProc mouseProc; // Делегат для обработки глобальных событий мыши
        private IntPtr hookID = IntPtr.Zero; // ID хука
        private const int MaxCalibrationPoints = 96; // Максимальное количество точек калибровки всего должно быть 16 x 5
        private const string CalibrationFilePath = "calibration.json"; // Путь к файлу калибровки


        private bool isSTOPPED = false; // чтобы остановить музыку.


        private int algorytm = 0; //алгоритм поиска нот



        private const int WM_HOTKEY = 0x0312; // Сообщение Windows для обработки горячих клавиш



        public Form1()
        {
            InitializeComponent();
            calibrationPoints = new List<Point>(); // Инициализация списка
            isCalibrating = false; // Изначально калибровка не запущена
            mouseProc = HookCallback; // Привязываем делегат к методу
            comboBox1.SelectedIndex = 0;
            this.KeyPreview = true;

            // Регистрируем горячую клавишу "Home" выйти
            RegisterHotKey(this.Handle, 1, 0, (int)Keys.Home);
            this.FormClosed += (sender, e) => UnregisterHotKey(this.Handle, 1); // Отменяем регистрацию при закрытии формы


            // Регистрируем горячую клавишу "Multiply" остановить
            RegisterHotKey(this.Handle, 2, 0, (int)Keys.Multiply);
            this.FormClosed += (sender, e) =>
            {
                // Освобождаем горячие клавиши при закрытии формы
                UnregisterHotKey(this.Handle, 1);
                UnregisterHotKey(this.Handle, 2);
            };

            // Пытаемся загрузить калибровочные данные при старте
            LoadCalibrationData();

            comboBox_algorytm.SelectedIndex = 0;

            button_debug.Hide();
            button1.Hide();
        }
        #region close app
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Обработка сообщений Windows, чтобы поймать нажатие зарегистрированной клавиши
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32(); // Идентификатор горячей клавиши

                if (id == 1)
                {
                    Application.Exit(); // Закрываем приложение
                }
                else if (id == 2) // 
                {
                    isSTOPPED = true; // остановить игру
                }
            }
        }
        #endregion




        #region Mouse_works

        // Обработчик глобальных кликов мыши
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN && isCalibrating)
            {
                var mouseHookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var point = new Point(mouseHookStruct.pt.X, mouseHookStruct.pt.Y);
                calibrationPoints.Add(point); // Сохраняем координаты клика

                if (calibrationPoints.Count >= MaxCalibrationPoints)
                {
                    stopCalibration(); // Останавливаем калибровку после 10 кликов
                }
            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        // P/Invoke для глобального перехвата мыши
        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }


        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public Point pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        #endregion

        #region save and calibrate
        // калибровка гитары
        private void button_calibrate_guitar_Click(object sender, EventArgs e)
        {
            calibrationPoints.Clear(); // Очищаем предыдущие точки
            isCalibrating = true; // Запускаем калибровку
            hookID = SetHook(mouseProc); // Устанавливаем глобальный хук для мыши
            this.Cursor = Cursors.Cross; // Меняем курсор на перекрестие для удобства
            this.Hide(); // скрыть форму на время калиброви
        }




        private void stopCalibration()
        {
            isCalibrating = false; // Останавливаем калибровку
            UnhookWindowsHookEx(hookID); // Удаляем хук
            this.Cursor = Cursors.Default; // Сбрасываем курсор
            this.Show(); //вернуть видимость формы
            // Сохраняем данные калибровки в файл
            SaveCalibrationData();
        }

        // Загрузка данных калибровки из файла при запуске
        private void LoadCalibrationData()
        {
            if (File.Exists(CalibrationFilePath))
            {
                try
                {
                    string json = File.ReadAllText(CalibrationFilePath);
                    calibrationPoints = JsonSerializer.Deserialize<List<Point>>(json);

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading calibration data: {ex.Message}");
                    calibrationPoints.Clear();
                }
            }
            else
            {
                MessageBox.Show("No calibration data. Please calibrate first.");
                calibrationPoints.Clear();
            }
        }

        //сохранить данные калибровки в файл
        private void SaveCalibrationData()
        {
            try
            {
                string json = JsonSerializer.Serialize(calibrationPoints);
                File.WriteAllText(CalibrationFilePath, json);
                MessageBox.Show("calibration saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving calibration data: {ex.Message}");
            }
        }


        // проверить калибровку гитары
        private async void button_test_calibration_Click(object sender, EventArgs e)
        {
            if (calibrationPoints.Count == 0)
            {
                MessageBox.Show("First perform the calibration!");
                return;
            }


            foreach (var point in calibrationPoints)
            {
                MouseSimulator.Click(point); // Симулируем клик мыши
                await Task.Delay(50); // Небольшая задержка между кликами
            }
        }
        #endregion


        #region prepare notes

        //список для хранения инструкций
        private List<string> instructionList_track1 = new List<string>();
        private List<string> instructionList_track2 = new List<string>();

        private List<string> finalized_instructions = new List<string>();
        private void AssenblyPreparedNotes()
        {
            //получить первичные инструкции для каждого трека
            var preleminary_instructions_track1 = prepare_instructions(richTextBox_notes1.Text);
            var preleminary_instructions_track2 = prepare_instructions(richTextBox_notes2.Text);


            // сгенерировать финальные инструкции для каждой дорожки            
            GenerateInstructionsFromNotes_track1(preleminary_instructions_track1.Item1.ToString(), preleminary_instructions_track1.Item2); // передаем инструкции
            GenerateInstructionsFromNotes_track2(preleminary_instructions_track2.Item1.ToString(), preleminary_instructions_track2.Item2); // передаем инструкции

        }

        //тут создаётся первичная инструкция. это кортеж, я так возвращаю сразу два значения
        private (StringBuilder, int) prepare_instructions(string input_notes)
        {
            // Преобразование текста из RichTextBox в одну строку
            string inputNotes = input_notes
                .Replace(Environment.NewLine, "") // Убираем переносы строк
                .Replace(" ", "") // Убираем пробелы
                .Replace("&", "") // Убираем символы &
                .Replace("+", "#") // Заменяем + на #
                .ToUpper(); // Приводим к верхнему регистру

            // Удаляем все пробелы
            string sanitizedNotes = string.Join("", inputNotes.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            // Начальные настройки
            int currentOctave = 4; // Начальная октава
            int tempo = 175; // Значение по умолчанию для темпа (удар/минуту)

            // Инструкции для игры
            StringBuilder instructions = new StringBuilder();

            // Пробегаем по каждой ноте в строке
            for (int i = 0; i < sanitizedNotes.Length; i++)
            {
                char note = sanitizedNotes[i];

                // Обработка темпа
                if (note == 'T' && i + 1 < sanitizedNotes.Length && char.IsDigit(sanitizedNotes[i + 1]))
                {
                    i++; // Переходим к цифрам
                    string tempoString = string.Empty;

                    // Считываем все цифры до следующего символа
                    while (i < sanitizedNotes.Length && char.IsDigit(sanitizedNotes[i]))
                    {
                        tempoString += sanitizedNotes[i];
                        i++;
                    }
                    i--; // Уменьшаем i, чтобы учесть инкремент в while

                    // Преобразуем строку в целое число и устанавливаем темп
                    if (int.TryParse(tempoString, out int parsedTempo))
                    {
                        tempo = parsedTempo;
                    }
                    continue; // Переход к следующей итерации цикла
                }

                // Обработка изменения октавы
                if (note == '>')
                {
                    currentOctave++;
                }
                else if (note == '<')
                {
                    currentOctave--;
                }
                else
                {
                    // Извлечение ноты и её длительности
                    string noteString = note.ToString();
                    int duration = 1; // Значение по умолчанию (длительность ноты)
                    i++; // Переходим к следующему символу

                    // Проверяем, является ли следующий символ диезом
                    if (i < sanitizedNotes.Length && sanitizedNotes[i] == '#')
                    {
                        noteString += '#'; // Добавляем диез к ноте
                        i++;
                    }

                    // Обработка длительности: считываем все цифры подряд
                    StringBuilder durationString = new StringBuilder();
                    while (i < sanitizedNotes.Length && char.IsDigit(sanitizedNotes[i]))
                    {
                        durationString.Append(sanitizedNotes[i]);
                        i++;
                    }
                    i--; // Уменьшаем i, чтобы учесть инкремент в while

                    // Преобразуем длительность из строки в целое число, если это возможно
                    if (durationString.Length > 0 && int.TryParse(durationString.ToString(), out int parsedDuration))
                    {
                        duration = parsedDuration; // Обновляем длительность, если она корректная
                    }

                    // Вычисление базовой длительности ноты
                    int noteDurationMs = 60000 / tempo * 4 / duration;

                    // Проверяем, есть ли точка в следующем символе
                    if (i + 1 < sanitizedNotes.Length && sanitizedNotes[i + 1] == '.')
                    {
                        // Если точка есть, добавляем половину времени к длительности
                        noteDurationMs += noteDurationMs / 2; // Увеличиваем длительность на половину
                        i++; // Пропускаем точку
                    }

                    // Добавляем инструкцию для ноты
                    instructions.AppendLine($"{currentOctave} {noteString} {noteDurationMs}");
                }
            }
            return (instructions, tempo);
        }




        private void GenerateInstructionsFromNotes_track1(string instructions, int tempo)
        {
            StringBuilder outputInstructions = new StringBuilder();
            instructionList_track1.Clear();

            // Обрабатываем каждую инструкцию
            var instructionLines = instructions.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in instructionLines)
            {
                var parts = line.Split(' ');
                if (parts.Length >= 3) // Проверяем, что есть октава, нота и длительность
                {
                    int octave = int.Parse(parts[0]);
                    string note = parts[1];
                    int duration = int.Parse(parts[2]); // Получаем длительность

                    // Проверяем на паузу R (которая теперь будет 'P')
                    if (note.StartsWith("R"))
                    {
                        // Пауза, добавляем 'P' и 0 для калибровочной точки
                        outputInstructions.AppendLine($"P 0 {duration}");
                        instructionList_track1.Add($"P 0 {duration.ToString()}");
                    }
                    else
                    {
                        // Получаем калибровочную точку для обычной ноты
                        int calibrationPoint = GetCalibrationPoint(octave, note);

                        if (calibrationPoint != -1)
                        {
                            // Получаем букву струны
                            char stringLetter = GetStringLetter(calibrationPoint);

                            // Формируем строку для вывода
                            outputInstructions.AppendLine($"{stringLetter} {calibrationPoint} {duration}");
                            instructionList_track1.Add($"{stringLetter.ToString()} {calibrationPoint.ToString()} {duration.ToString()}");
                        }
                        else
                        {
                            // Если нота не найдена, корректируем октаву
                            if (octave <= 2)
                            {
                                octave = 3; // Меняем октаву на 3, если октава 2 или ниже
                            }
                            else if (octave >= 5)
                            {
                                octave = 4; // Меняем октаву на 4, если октава 5 или выше
                            }

                            // Повторяем попытку найти калибровочную точку с новой октавой
                            calibrationPoint = GetCalibrationPoint(octave, note);
                            if (calibrationPoint != -1)
                            {
                                char stringLetter = GetStringLetter(calibrationPoint);
                                outputInstructions.AppendLine($"{stringLetter} {calibrationPoint} {duration}");
                                instructionList_track1.Add($"{stringLetter.ToString()} {calibrationPoint.ToString()} {duration.ToString()}");
                            }
                            else
                            {
                                // Если нота всё ещё не найдена, выводим X с длительностью
                                outputInstructions.AppendLine($"X 0 {duration}");
                                instructionList_track1.Add($"X 0 {duration.ToString()}");
                            }
                        }
                    }
                }
            }

        }
        private void GenerateInstructionsFromNotes_track2(string instructions, int tempo)
        {
            StringBuilder outputInstructions = new StringBuilder();
            instructionList_track2.Clear();

            // Обрабатываем каждую инструкцию
            var instructionLines = instructions.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in instructionLines)
            {
                var parts = line.Split(' ');
                if (parts.Length >= 3) // Проверяем, что есть октава, нота и длительность
                {
                    int octave = int.Parse(parts[0]);
                    string note = parts[1];
                    int duration = int.Parse(parts[2]); // Получаем длительность

                    // Проверяем на паузу R (которая теперь будет 'P')
                    if (note.StartsWith("R"))
                    {
                        // Пауза, добавляем 'P' и 0 для калибровочной точки
                        outputInstructions.AppendLine($"P 0 {duration}");
                        instructionList_track2.Add($"P 0 {duration.ToString()}");
                    }
                    else
                    {
                        // Получаем калибровочную точку для обычной ноты
                        int calibrationPoint = GetCalibrationPoint(octave, note);

                        if (calibrationPoint != -1)
                        {
                            // Получаем букву струны
                            char stringLetter = GetStringLetter(calibrationPoint);

                            // Формируем строку для вывода
                            outputInstructions.AppendLine($"{stringLetter} {calibrationPoint} {duration}");
                            instructionList_track2.Add($"{stringLetter.ToString()} {calibrationPoint.ToString()} {duration.ToString()}");
                        }
                        else
                        {
                            // Если нота не найдена, корректируем октаву
                            if (octave <= 2)
                            {
                                octave = 3; // Меняем октаву на 3, если октава 2 или ниже
                            }
                            else if (octave >= 5)
                            {
                                octave = 4; // Меняем октаву на 4, если октава 5 или выше
                            }

                            // Повторяем попытку найти калибровочную точку с новой октавой
                            calibrationPoint = GetCalibrationPoint(octave, note);
                            if (calibrationPoint != -1)
                            {
                                char stringLetter = GetStringLetter(calibrationPoint);
                                outputInstructions.AppendLine($"{stringLetter} {calibrationPoint} {duration}");
                                instructionList_track2.Add($"{stringLetter.ToString()} {calibrationPoint.ToString()} {duration.ToString()}");
                            }
                            else
                            {
                                // Если нота всё ещё не найдена, выводим X с длительностью
                                outputInstructions.AppendLine($"X 0 {duration}");
                                instructionList_track2.Add($"X 0 {duration.ToString()}");
                            }
                        }
                    }
                }
            }

        }

        private int GetCalibrationPoint(int octave, string note)
        {
            

            if (comboBox_algorytm.SelectedIndex == 0) 
            {
                // Создаём ключ в формате "октава+нота"
                string key = $"{octave}{note}";

                // Проверяем, в какой струне ищем ноту и возвращаем соответствующую калибровочную точку
                if (guitarStrings.String6.TryGetValue(key, out int calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 6 струны
                }
                else if (guitarStrings.String5.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 5 струны
                }
                else if (guitarStrings.String4.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 4 струны
                }
                else if (guitarStrings.String3.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 3 струны
                }
                else if (guitarStrings.String2.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 2 струны
                }
                else if (guitarStrings.String1.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 1 струны
                }

                // Если октава или нота не найдены, возвращаем -1
                return -1;
            }
            else if (comboBox_algorytm.SelectedIndex == 1)
            {
                // Создаём ключ в формате "октава+нота"
                string key = $"{octave}{note}";

                // Проверяем, в какой струне ищем ноту и возвращаем соответствующую калибровочную точку
                if (guitarStrings.String1.TryGetValue(key, out int calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 6 струны
                }
                else if (guitarStrings.String2.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 5 струны
                }
                else if (guitarStrings.String3.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 4 струны
                }
                else if (guitarStrings.String4.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 3 струны
                }
                else if (guitarStrings.String5.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 2 струны
                }
                else if (guitarStrings.String6.TryGetValue(key, out calibrationPoint))
                {
                    return calibrationPoint; // Возвращаем калибровочную точку для 1 струны
                }

                // Если октава или нота не найдены, возвращаем -1
                return -1;
            }
            else
            {
                return -1;
            }
            ;
        }
        
        // Метод для определения буквы струны на основе калибровочной точки
        private char GetStringLetter(int calibrationPoint)
        {
            double dividedPoint = calibrationPoint / 16.0;

            if (dividedPoint > 0 && dividedPoint <= 1)
                return 'Q'; // Струна 6
            else if (dividedPoint > 1 && dividedPoint <= 2)
                return 'W'; // Струна 5
            else if (dividedPoint > 2 && dividedPoint <= 3)
                return 'E'; // Струна 4
            else if (dividedPoint > 3 && dividedPoint <= 4)
                return 'R'; // Струна 3
            else if (dividedPoint > 4 && dividedPoint <= 5)
                return 'T'; // Струна 2
            else if (dividedPoint > 5 && dividedPoint <= 6)
                return 'Y'; // Струна 1
            else
                return ' '; // Если не попадает в диапазон, возвращаем пробел
        }

        #endregion




        private async void button_play_Click(object sender, EventArgs e)
        {
            AssenblyPreparedNotes();


            InstructionCombiner combiner = new InstructionCombiner();
            var newTrack = combiner.CombineInstructions(instructionList_track1, instructionList_track2);

            finalized_instructions = newTrack;

            // Проверка наличия инструкций 
            if ((finalized_instructions == null || finalized_instructions.Count == 0))
            {
                MessageBox.Show("No MML to play.");
                return;
            }
            else
            {
                await PlayTrack(newTrack);
            }
        }




        //int play_delay = 1;
        private async Task PlayTrack(List<string> instructions)
        {


            // Проходим по каждой инструкции в списке
            for (int i = 0; i < instructions.Count; i++)
            {
                if (isSTOPPED == true)
                {
                    isSTOPPED = false;
                    return;
                }
                var instruction = instructions[i];
                var parts = instruction.Split(' '); // Разделяем инструкцию на части

                if (parts.Length >= 3) // Убедимся, что есть хотя бы три части
                {
                    string key = parts[0]; // Буква клавиши (Q W E R T Y или P для паузы)

                    if (key == "Z" && parts.Length >= 6) // Обработка инструкций Z
                    {
                        //Z Q 24 322 W 35 322
                        // Извлекаем необходимые параметры
                        int calibrationIndex1 = int.Parse(parts[2]); // 3-й элемент (первая точка)
                        int calibrationIndex2 = int.Parse(parts[5]); // 6-й элемент (вторая точка)
                        int duration = int.Parse(parts[3]); // 4-й элемент (время)

                        // Получаем точки калибровки
                        if (calibrationIndex1 == 0 && calibrationIndex2 > 0) // играть только 1
                        {
                            Point calibrationPoint2 = calibrationPoints[calibrationIndex2 - 1];

                            // Перемещаем курсор на первую точку
                            //MouseSimulator.Click(calibrationPoint1);
                            //await Task.Delay(2); // Задержка на перемещение мыши

                            // Перемещаем курсор на вторую точку
                            MouseSimulator.Click(calibrationPoint2);
                            //await Task.Delay(play_delay); // Задержка на перемещение мыши

                            // Одновременно нажимаем клавиши, указанные в инструкции
                            // SendKeys.SendWait(parts[1]); // 2-й элемент (первая клавиша)
                            SendKeys.SendWait(parts[4]); // 5-й элемент (вторая клавиша)

                            // Выжидаем указанную задержку
                            await Task.Delay(duration); // Задержка первой инструкции

                            // Отпускаем первую клавишу
                            //MouseSimulator.Click(calibrationPoint1);
                            //await Task.Delay(2); // Задержка на перемещение мыши
                            // Отпускаем вторую клавишу
                            MouseSimulator.Click(calibrationPoint2);
                        }
                        else if (calibrationIndex1 > 0 && calibrationIndex2 == 0) // играть только 2
                        {
                            Point calibrationPoint1 = calibrationPoints[calibrationIndex1 - 1];


                            // Перемещаем курсор на первую точку
                            MouseSimulator.Click(calibrationPoint1);
                            //await Task.Delay(play_delay); // Задержка на перемещение мыши


                            // Одновременно нажимаем клавиши, указанные в инструкции
                            SendKeys.SendWait(parts[1]); // 2-й элемент (первая клавиша)


                            // Выжидаем указанную задержку
                            await Task.Delay(duration); // Задержка первой инструкции

                            // Отпускаем первую клавишу
                            MouseSimulator.Click(calibrationPoint1);
                            //await Task.Delay(2); // Задержка на перемещение мыши
                            // Отпускаем вторую клавишу

                        }
                        else
                        {
                            Point calibrationPoint1 = calibrationPoints[calibrationIndex1 - 1];
                            Point calibrationPoint2 = calibrationPoints[calibrationIndex2 - 1];

                            // Перемещаем курсор на первую точку
                            MouseSimulator.Click(calibrationPoint1);
                            //await Task.Delay(play_delay); // Задержка на перемещение мыши

                            // Перемещаем курсор на вторую точку
                            MouseSimulator.Click(calibrationPoint2);
                            //await Task.Delay(play_delay); // Задержка на перемещение мыши

                            // Одновременно нажимаем клавиши, указанные в инструкции
                            SendKeys.SendWait(parts[1]); // 2-й элемент (первая клавиша)
                            SendKeys.SendWait(parts[4]); // 5-й элемент (вторая клавиша)

                            // Выжидаем указанную задержку
                            await Task.Delay(duration); // Задержка первой инструкции

                            // Отпускаем первую клавишу
                            MouseSimulator.Click(calibrationPoint1);
                            //await Task.Delay(play_delay); // Задержка на перемещение мыши
                            // Отпускаем вторую клавишу
                            MouseSimulator.Click(calibrationPoint2);
                        }

                    }
                    else if (int.TryParse(parts[1], out int calibrationIndex) && int.TryParse(parts[2], out int duration))
                    {
                        if (calibrationIndex == 0) // Если это пауза
                        {
                            await Task.Delay(duration); // Держим паузу
                        }
                        else if (calibrationIndex > 0 && calibrationIndex <= calibrationPoints.Count) // Если это нота
                        {
                            Point calibrationPoint = calibrationPoints[calibrationIndex - 1]; // Получаем точку калибровки

                            // Задержка на перемещение мыши 
                            //await Task.Delay(play_delay);

                            // Нажимаем на калибровочную точку
                            MouseSimulator.Click(calibrationPoint);

                            // Нажимаем клавишу струны
                            SendKeys.SendWait(key); // Нажимаем нужную клавишу

                            // Держим длительность ноты
                            await Task.Delay(duration);

                            // Снова нажимаем на калибровочную точку, чтобы отпустить струну
                            MouseSimulator.Click(calibrationPoint);
                        }
                    }
                }
            }
        }

        //смена алгоритма
        private void comboBox_algorytm_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ( comboBox_algorytm.SelectedIndex == 0)
            {
                algorytm = 1;
            }
            if (comboBox_algorytm.SelectedIndex == 1)
            {
                algorytm = 2;
            }
        }



        //кнопка для приведения нот в упрощённый вид. 
        private void button1_Click(object sender, EventArgs e)
        {
            string inputNotes = "";
            string inputNotes1 = "";
            foreach (string line in richTextBox_notes1.Lines)
            {
                inputNotes += line.Replace(" ", "")
                                  .Replace("&", "")
                                  .Replace("+", "#")
                                  .ToUpper(); // Приводим к верхнему регистру
            }
            foreach (string line in richTextBox_notes2.Lines)
            {
                inputNotes1 += line.Replace(" ", "")
                                  .Replace("&", "")
                                  .Replace("+", "#")
                                  .ToUpper(); // Приводим к верхнему регистру
            }

            richTextBox_notes1.Text = inputNotes;
            richTextBox_notes2.Text = inputNotes1;

        }

        #region Pre-recorded melody
        //выбрана мелодия
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex; // Получаем индекс выбранного элемента

            switch (selectedIndex)
            {
                case 0: // не выбрано
                    richTextBox_notes1.Text = "";
                    richTextBox_notes2.Text = "";
                    break;

                case 1: // bad apple
                    richTextBox_notes1.Text = "T175<E8>E16E8E16D16E16<E8>E16E8E16D16E16<E8>E16E8E16D16E16<E8>D16E16A8E16D16<E8>E16E8E16D16E16<E8>E16E8E16D16E16<E8>E16E8E16D16E16A8G16A16G8E16G16<E8>E16E8E16D16E16<E8>E16E8E16D16E16<E8>E16E8E16D16E16<E8>D16E16A8E16D16<E8>E16E8E16D16E16<E8>E16E8E16D16E16<E8>E16E8E16D16E16A16.G16A16.G4E8F#8G8A8B4>E8D8<B4E4B8A8G8F#8E8F#8G8A8B4A8G8F#8E8F#8G8F#8E8D#8F#8E8F#8G8A8B4>E8D8<B4E4B8A8G8F#8E8F#8G8A8B4A8G8F#8R8G8R8A4B4E8F#8G8A8B4>E8D8<B4E4B8A8G8F#8E8F#8G8A8B4A8G8F#8E8F#8G8F#8E8D#8F#8E8F#8G8A8B4>E8D8<B4E4B8A8G8F#8E8F#8G8A8B4A8G8F#8R8G8R8A4B4>D8E8<B8A8B4A8B8>D8E8<B8A8B4A8B8A8G8F#8D8E4D8E8F#8G8A8B8E4B8>D8D8E8<B8A8B4A8B8>D8E8<B8A8B4A8B8A8G8F#8D8E4D8E8F#8G8A8B8E4B8>D8D8E8<B8A8B4A8B8>D8E8<B8A8B4A8B8A8G8F#8D8E4D8E8F#8G8A8B8E4B8>D8D8E8<B8A8B4A8B8>D8E8<B8A8B4>E8F#8G8F#8E8D8<B4A8B8A8G8F#8D8E4D4";
                    richTextBox_notes2.Text = "T175<R1R1R1R1R1R1R1R1<E1G1A1F#1E1G1A1F#8R8>D8R8<A4B4E1G1A1F#1E1G1A1F#8R8>D8R8<A4B4>>C1D1E1R2.D4C1D1E1R2<B16.B16B16.A16.A16A16.>C1D1E1R2.D4C1D1E1R2<B16.B16B16.A16.A16A16.>C1";
                    comboBox_algorytm.SelectedIndex = 0;
                    break;

                case 2: // coffin dance
                    richTextBox_notes1.Text = "T140C8C8C8C8F8F8F8F8G8G8G8G8G8G8G8G8G8G8G8G8C8<A#8A8F8G4G8>D8C4<A#4A4A8A8>C4<A#8A8G4G8>A#8A8A#8A8A#8<G4G8>A#8A8A#8A8A#8<G4G8>D8C4<A#4A4A8A8>C4<A#8A8G4G8>A#8A8A#8A8A#8<G4G8>A#8A8A#8A8A#8<G4G8>D8C4<A#4A4A8A8>C4<A#8A8G4G8>A#8A8A#8A8A#8<G4G8>A#8A8A#8A8A#8";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 0;
                    break;

                case 3: // crab rave
                    richTextBox_notes1.Text = "t175d8a+8g8g16d8d16a8f8f16d8d16a8f8f16c8c8e8e16f8d8a+8g8g16d8d16a8f8f16d8d16a8f8f16c8c8e8e16f8<d8a+8g8g16d8d16a8f8f16d8d16a8f8f16c8c8e8e16f8>d8a+8g8g16d8d16a8f8f16d8d16a8f8f16c8c8e8e16f8d8a+8g8g16d8d16a8f8f16d8d16a8f8f16c8c8e8e16f8d8a+8g8g16d8d16a8f8f16d8d16a8f8f16c8c8e8e16f8<d8a+8g8g16d8d16a8f8f16d8d16a8f8f16c8c8e8e16f8>d8a+8g8g16d8d16a8f8f16d8d16a8f8f16c8c8e8e16f8";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 0;
                    break;

                case 4: // megaman
                    richTextBox_notes1.Text = "T200R8E16E16E8E16E16E8C#4C#16C#16E8E16E16E8C#4G#8F#8G#4E16E16E8E16E16E8C#4G#4F#4E4F#2F#16F#16F#8F#16F#16F#8D#4G#4F#4E4D#4C#4>C#8G#8B8A#2A#8C#8G#8B8A#4B8>C#8T175R8<<E16E16E8E16E16E8C#4C#16C#16E8E16E16E8C#4G#8F#8G#4E16E16E8E16E16E8C#4G#4F#4E4F#2F#16F#16F#8F#16F#16F#8D#4G#4F#4E4D#4C#4>C#8G#8B8A#2A#8C#8G#8B8A#4B8>C#2.<C#8<B8>E4C#4<B4>C#4.<B2B8>C#4.<G#8A8G#8E4E8G#8B8>C#2.<B8>E4C#4<B4>C#4<B2B8B8G#8B8>C4.";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;

                case 5: // Night of nights
                    richTextBox_notes1.Text = "T178D16<B16F#16>D16<B16F#16>D16<B16F#16>D16<B16F#16>C#8C#8C#8D16<B16F#16>D16<B16F#16>D16<B16F#16>D16<B16F#16>E8E8E8D16<B16F#16>D16<B16F#16>D16<B16F#16>D16<B16F#16>C#8C#8C#8D16<B16F#16>D16<B16F#16>D16<B16F#16>D16<B16F#16>E8E8E8D2C#2F2E2D2C#2F2E2F#4D4F4C#4G#4F4E4G4F#4D4F4C#4G#4F4E2<B4>F#4C#4F#4D4E8F#8E4A4B8F#8>C#8D8C#8D16C#16<B8A8F#8A8E8F#8D2<B4>F#4C#4F#4D4E8F#8E4A4B8F#8>C#8D8C#8D16C#16<B8A8B1<B4>F#4C#4F#4D4E8F#8E4A4B8F#8>C#8D8C#8D16C#16<B8A8F#8A8E8F#8D2<B4>F#4C#4F#4D4E8F#8E4A4B8F#8>C#8D8C#8D16C#16<B8A8B1B16F#16E16B16F#16E16B16F#16E16B16F#16E16B16F#16E16B16F#16E16B16F#16E16B16F#16E16A#16F16D#16A#16F16D#16A#16F16D#16A#16F16D#16A#16F16D#16A#16F16D#16A#16F16D#16A#16F16D#16A#16F16D#16A#16F16D#16G#16D#16C#16G#16D#16C#16F#16C#16<B16>F16F#16G#16D#4D#16<B16>C#16D#16<B16F#16>D#16<B16F#16>F16C#16<G#16>F16C#16<G#16>F16D16<G#16>F16F#16G#16A#4A#16F#16G#16A#16F16D#16A#16F16D#16G#16D#16C#16G#16D#16C#16F#16C#16<B16>F16F#16G#16D#4D#16<B16>C#16D#16<B16F#16>D#16<B16F#16>F16C#16<G#16>F16C#16<G#16>F16D16<G#16>F16F#16G#16A#8A#32A#8A#8A#32D2C#2F2E2D2C#2F2E2F#4D4F4C#4G#4F4E4G4F#4D4F4C#4G#4F4E2<B4>F#4C#4F#4D4E8F#8E4A4B8F#8>C#8D8C#8D16C#16<B8A8F#8A8E8F#8D2<B4>F#4C#4F#4D4E8F#8E4A4B8F#8>C#8D8C#8D16C#16<B8A8B1";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;

                case 6: //titan
                    richTextBox_notes1.Text = "t160r1>d8d8c+16<b16f+16>d8d8c+8<b16f+8>d8d8c+16<b16f+16>d8d8c+8<b16f+8>d8d8c+16<b16f+16>d8d8c+8<b16f+8>d8d8c+16<b16f+16>d8d8c+8<b16f+8b4b16>d16d16c+8c+8<b16>c+8<b16>c+2.&c+16<b16>c+8d16c+4<b4.b16>c+8<b8>c+16c+4<f+2.b4b16>d16d16c+8c+8<b16>c+8<b16>c+2.&c+16<b4>g4d8d8c+4<b4>f+4d8e8c+4<a+4>f+4g4f+4.<b8>c+4d4g2f+2<b8>c+8d8<f+8g2f+2";
                    richTextBox_notes2.Text = "t160r1<<b4b4b4b4b4b4b4b4g4g4g4g4a4a4a4a4b8b8>b8<b8>>c+8<<b8>b8<b8a8a8>a8<a8>>c+8<<a8>a8<a8g8g8>g8<g8>b8<g8>g8<g8f+8f+8>f+8<f+8>a8<f+8>f+8r8<b8b8>b8<b8>>c+8<<b8>b8<b8a8a8>a8<a8>>c+8<<a8>a8<a8g8g8>g8<g8>b8<g8>g8<g8f+2>c+4r4<<b16>b16>d16<b16<b16>b16>d16<b16<b16>b16>d16<b16<b16>b16>d16<b16<a16>a16>c+16<a16<a16>a16>c+16<a16<a16>a16>c+16<a16<a16>a16>c+16<a16<g16>g16>d16<g16<g16>g16>d16<g16<g16>g16>d16<g16<g16>g16>d16<g16<f+16>f+16>c+16<f+16<f+16>f+16>c+16<f+16f+2";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 7: //red march
                    richTextBox_notes1.Text = "t160f4f4f16f16f4f8f4f8.f16f16f16f4f1&f1&f8d4<a4>d8c16<a+16a4g8g16>f16<g8>e8<a8a16a+16a16a16a16a16>a16<a16>e16<a16>f16<a16>d16<a16>g16<a16>f16<a16>g16<a16>e16<a16>a16<a16>g16<a16>a16<a16>f16<a16>g16<a16>f16<a16>g16<a16>e16<a16>f16<a16>e16<a16>f16<a16>d16<a16>g16<a16>f16<a16>g16<a16>e16<a16>a16<a16>g16<a16>a16<a16>f16<a16>g16<a16>f16<a16>g16<a16>e16<a16>d1&d8f4f4f4f8d4d8.c+16d8<a4>d8e4e8.d16e4.e8f4f8.g16a8f4g8a4a8.g16a4.e8a4a8.g16a8d4a8a+4a+8.a16g4.g8a4f4g4e8d8c+4c+8d8e8<a4a8>d4d8.c+16d8<a4>d8e4e8.d16e4>>d8.e16<<f4f8.g16a8f4g8a4a8.g16a4.a8d4d4d8d4d8g4g8.d16d4>>a+8<<d8a4f4g4e8d8c+4c+8d8e8a4.";
                    richTextBox_notes2.Text = "t160<d4<a4>d4<a4>d4<a4>d4<a4d4a4a+8a16g16a4g8g16f16g8e8c+16d16e8e8<a8>d4a4a+8a16g16a4a+8a+16a16a+8g8>c+16d16e8e8e16e16<<a+8>>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8<d8>d8d4<a4>d4<a4>d4<a4>d4<a4>d8a8<a8>a8d8f8<a8>a8c+8a8<a8>a8c+8a8<a8>a8f8>c8<c8>c8<f8>c8<c8>c8<e8a8<a8>a8e8a8<a8>a8d8a8<a8>a8d8a8<a8>a8<g8>a+8d8a+8g8a+8d8a+8f4d4e4c+8<b8<a4a8b8>c+8<a8b8>c+8>d8a8<a8>a8d8f8<a8>a8c+8a8<a8>a8c+8a8<a8>a8f8>c8<c8>c8<f8>c8<c8>c8<e8a8<a8>a8e8a8<a8>a8d8a8<a8>a8d8a8<a8>a8<g8>a+8d8a+8g8a+8d8a+8a4f4g4e8d8<<a4a8b8>c+8<a8b8>c+8";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 8: // прогноз погоды
                    richTextBox_notes1.Text = "T210E4E4E4.D8C4C4C4.<B8A4A4G#8.A16B8.A16G4F4F2>F4F4F4.E8D4D4D4.C8<B4B4A#8.B16>E8.D16C1E4E4E4.D8C4C4C4.<B8A4A4G#8.A16B8.A16>G4F4F2F4F4F4.D8E4C4<A4.>C8<B4B4B8.>D16C8.<B16>A2.<A4>G2F4.D8F2E4.C8E2D4.<B8>D4C4<B8.>C16D8.E16G2F4.D8F2E4.C8<B4B4>E8.D16<B8.>C16<A1";
                    richTextBox_notes2.Text = "T210<<A4.A8E4E4A4.A8E4E4A4.A8E4E4D4.D8A4A4D4.D8A4A4D4.D8A4A4E4.E8B4E4A4.A8E8.E16F#8.G#16A4.A8E4E4A4.A8E4E4A4.A8E4E4D4.D8A4A4A#4.A#8F4F4A4G#4G4F4E4.E8B4E4A4.A8A2D4D2D4A4A2A4E4E2E4A4A2A4D4D2D4A4A2A4E4E2E4A4.A8E8.E16F#8.G#16A1";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 9: //EL bimbo
                    richTextBox_notes1.Text = "T140R4.F8>C#8C8<A#8F8C#1R4.F8>C#8C8<A#8F8G#4.F#8F#2.R8D#8F8F#8G#8A#8>C#4.C8C4.<A#8A#4.G#8G#4.F#8F#4.F8F2R4.F8>C#8C8<A#8F8C#1R4.F8>C#8C8<A#8F8G#4.F#8F#2.R8D#8F8F#8G#8A#8>C#4.C8C4.<A#8A#4.G#8G#4.F#8F#4.F8F2R4.F8G#4.G#32R16.F4.D#16R16D#2.D#16R16F8F#4.F8";
                    richTextBox_notes2.Text = "T140R4.<F8>C#8C8<A#8F8C#8R1R4F8>C#8C8<A#8F8G#8R4F#8F#8R2.D#8F8F#8G#8A#8>C#8R4C8C8R4<A#8A#8R4G#8G#8R4F#8F#8R4F8F2R4.F8>C#8C8<A#8F8C#8R1R4F8>C#8C8<A#8F8G#8R4F#8F#8R2.D#8F8F#8G#8A#8>C#8R4C8C8R4<A#8A#8R4G#8G#8R4F#8F#8R4F8F2R4.F8G#8R4F#8F8R4D#8D#8R2.F8F#8R4F8";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 10: // sweden
                    richTextBox_notes1.Text = "T93<G2<F#2>>D2<<B2>A2<G2>>C#1<B2<F#2>>F#2<<B2>>C#2<<G2>>E1<B2<F#2>>F#2<<B2>>C#2<<G2>>E1<B2>A4B4F#2<<B4>>D8E8C#2<<G4>>F#8A8E1<B4>>D4E4<A4F#2<<B4>>D8E8C#2<<G4>>A8F#8E1<B2>A4B4>D2<<<B4>>>F#8E8C#2<<<G4>>>D8C#8<A1<B2>B4A4F#2<<B4>>D8E8C#2<<G4>>F#8A8E1<B2>A4B4F#2<<B4>>D8E8C#2<<G4>>F#8A8E1<B2>A4B4>D2<<<B4>>D8E8>C#2F#4<F#8A8E1F#2.B8A8E2E4D4C#2.D8E8<B1>F#2.B8A8E2>E4D4C#2D4F#8<E8B1";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 11: // mozarella
                    richTextBox_notes1.Text = "T135<<F16R8>C16R8<F16R8>C16R8<F16R8>C16R8<F16R8>C16R8<F16R8>C16R8<F16R8>C16R8<F16R8>C16R8<F16R32>>2D#16R32<<F16R8>>A16R32E16R32<<F16R32>>C16R32<C16R32>D#16R32<<F16R8>C16R32>E16R32<<F16R32>>C16R32<C16R32>E16R32F16R32<A#16R32>D16R32F16R32A16R32>C16R32<<D16R32>B16R32<<G16R32>>F16R32<D16R8<G16R32>>C16R32D16R32D#16R32E16R8A16R32E16R32<<F16R32>>C16R32<C16R32>D#16R32<<F16R8>>A16R32E16R32<<F16R32>>C16R32<C16R32>E16R32F16R32<A#16R32>D16R32F16R32A16R32>C16<R32<<D16R32>B16R32A#16R32F1616R8>D16R8>C16R32C16R32C16R32C16R8F16R8<A16R32>C16R32C16R32C16R32C16R8F16R8<A16R32>C16R32C16R32C16R32C16R8<D16R8F16R32E16R8.R32<C16R8>C16R32D16R32D#16R32E16R8A16R32E16R32<<F16R32>>C16R32D16R32D#16R32E16R8A16R32E16R32<<F16R32>>C16R32D16R32E16R32F16R32<A#16R32>D16R32F16R32A16R32>C16R32<<D16R32>B16R32A#16R32F16R32<D16R8<G16R32>>C16R32D16R32D#16R32E16R8A16R32E16R32<<F16R32>>C16R32D16R32D#16R32E16R8A16R32E16R32<<F16R32>>C16R32D16R32E16R32F16R32<A#16R32>D16R32F16R32A16R32>C16R32<<D16R32>B16R32A#16R32F16R32<D16R8<G16R8>D16R8<A16R32>>>C16R32<<G16R32>>C16R32<<<A16R8>G16R32>>C16R32<<<G#16R32>>>C16R32<<F#16R32>>C16R32<<<G#16R8>F#16R32>>C16R32<<<G16R32>>A#16R32<F16R32>A#16R32<<G16R32>>A16R32<F16R32>B16R32<C16R8G16R32>G16R32<C16R8G16R8<A16R32>>>C16R32<<G16R32>>C16R32<<<A16R8>G16R32>>C16R32<<<G#16R32>>>C16R32<<F#16R32>>C16R32<<<G#16R8>F#16R32>>C16R32<<<G16R32>>A#16R32<F16R32>A#16R32<<G16R32>>A16R32<F16R32>>D16R32C16R32<<C16R32C16R32>>E16R8<C16R32D16R32D#16R32<<F16R8>>A16R32E16R32<<F16R32>>C16R32<C16R32>D#16R32<<F16R8>C16R32>E16R32<<F16R32>>C16R32<C16R32>E16R32F16R32<A#16R32>D16R32F16R32A16R32>C16R32<<D16R32>B16R32<<G16R32>>F16R32<D16R8<G16R32>>C16R32D16R32D#16R32E16R8A16R32E16R32<<F16R32>>C16R32<C16R32>D#16R32<<F16R8>>A16R32E16R32<<F16R32>>C16R32<C16R32>E16R32F16R32<A#16R32>D16R32F16R32A16R32>C16R32<<D16R32>B16R32A#16R32F16R32<D16R8<G16R8>D16R8>>C16R32C16R32C16R32C16R8F16R8<A16R32>C16R32C16R32C16R32C16R8F16R8<A16R32>C16R32C16R32C16R32C16R8<D16R8F16R32E16";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 12: // futurama
                    richTextBox_notes1.Text = "T150D4.D8F#4E4E4E4R8A8A4D4D4R8E8E4E4E4R8A8A4D4D4R8G8G8F#8E4.E8A4G#4D4.D8F#4E4E4.E8A4G#4B4.B8G8G8F#8F#8R1R8>G#16G#16G#8D#8D#8<B8B8G#8";
                    richTextBox_notes2.Text = "T150<E4E8R16E16A16R16A16R16A8R16A16B4B8R16B16A32R16.A32R16.A8B8E4E8.E16A16R16A16R16A8R16A16B4B8R16B16G16R16G16R16G8F#8E4E8R16E16R8A16R16A8R16A16B4B8R16B16A16R16A16R16A16R16A32R32A32R32E4E8R16E16A16R16A16R16A16R8A16B4B8R16B16G16R16G16R16F#16R16F#16R16E8>D8E8<A4>D8E8<B16R16B8R8>F#8R8<A8R8>E8R8<E1";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 13: // nyeh 
                    richTextBox_notes1.Text = "T150R1R1R1R1R16.C16R16.G16R16>C16R16D#4C4<B8>C16R16D4C16R16<G16R16D#16R16C16R16D8D#16R16F#4G16R16D#16R16C16R16<A16R16B8>C16R16D4C16R2R16C16R16G16R16>C16R16D#8.F#32G32A#4A16R16G16R16A4A#16R16G16R16D#16R16C16R16<B16R16>C16R16D4D#16R16C16R16<G16R16D#16R16B8A16R16B32>C32<B32>C32<B8>C16R16R4R4<F16R16G#16R16>C16R16F4C4<B8>C16R16C#32C32C#32D8D32<B16R16G16R16B8>D8D#8.D32D#32D16R16C16R16<>C4<A#16R16G#16R16G8.G#32G32F16R16G#16R16G2R8<F16R16G#16R16>C16R16F4C4<B8>C16R16C#32C32C#32D8D32<B16R16G16R16B8>D8D#8.D32D#32D16R16C16R16>C8.C32C#32D16R16D#8D16R16C16R16<B16R16G#16R16F32F#32G8.<<D#16R16D16";
                    richTextBox_notes2.Text = "T150<<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<C8>D#32R16.<C8>A32R16.<C8>G32R16.<C8>F#32R16.<F8>F32R16.C8C32R16.<C8>F32R16.C8C32R16.<G8>D32R16.D8D32R16.<D8>G32R16.D8G32R16.<G#8>D#32R16.<D#8>D#32R16.<G#8>G#32R16.<F8>D#32R16.<D#8>D#32R16.<<A#8>>D#32R16.<C#8>>C#32R16.<<G8>G32R16.<F8>F32R16.C8C32R16.<C8>F32R16.C8C32R16.<G8>D32R16.D8D32R16.<D8>G32R16.D8G32R16.<G#8>D#32R16.<G#8>G#32R16.<G#8R4.>F4D4F32F#32G16F8D#8D8";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 14: //und: snows
                    richTextBox_notes1.Text = "T150D2R4<D8G8A4B4A32B32A8.G4>D2R4<D8G8A4B4A32B32A8.G4>C#2R4<C#8E8F#4G4A32B32A4A16>D8<B2R1R2>D2R4<D8G8A4B4A32B32A8.G4>D2R4<D8G8A4B4A32B32A8.G4>C#2R4<E8F#8A4B4>E8D4C#8<A2R1R2>D2R4<D8G8A4B4A32B32A8.G4>D2R4<D8G8A4B4A32B32A8.G4>C#2R4<C#8E8F#4G4A32B32A4A16>D8<B2R1R2>D2R4<D8G8A4B4A32B32A8.G4>D2R4<D8G8A4B4A32B32A8.G4>C#2R4<E8F#8A4B4>E8D4C#8<A2R1R2D2R4<A#8>C8D4F4A#4R8A8G8F8D4R1R2C#2R4<A8B8>C#4E4A4R8B8F#4R1R1R2<A#8>C8D4F4A#4R8A8G8F8D4C4F4E32D#32D4.D16R1R4<A8B8>C#8R8E8R8A8R4F#8G#8R2R8<B8>C#8D#8R8F#8R8B8R4G#8A#8";
                    richTextBox_notes2.Text = "T150<<C16R4.R16B16R4.R16C16R4.R16B16R4.R16C16R4.R16B16R4.R16C16R4.R16B16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16C16R4.R16B16R4.R16C16R4.R16B16R4.R16C16R4.R16B16R4.R16C16R4.R16B16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16C16R4.R16B16R4.R16C16R4.R16G16R4.R16C16R4.R16B16R4.R16C16R4.R16G16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>A16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16C16R4.R16B16R4.R16C16R4.R16G16R4.R16C16R4.R16B16R4.R16C16R4.R16B16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16<B16R4.R16>F#16R4.R16D#16R4.R16A#16R4.R16D#16R4.R16>C16R4.R16<D#16R4.R16A#16R4.R16D#16R4.R16>C16R4.R16<D16R4.R16A16R4.R16D16R4.R16>E16R4.R16<D16R4.R16A16R4.R16D16R4.R16B16R4.R16D#16R4.R16A#16R4.R16D#16R4.R16>C16R4.R16<D#16R4.R16A#16R4.R16D#16R4.R16>C16R4.R16<D16R4.R16A16R4.R16D16R4.R16>E16R4.R16<E16R4.R16B16R4.R16E16R4.R16>C#16R4.R16<F#16R4.R16>C#16R4.R16<F#16R4.R16>F#8R8<G#8R8>F#16";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 15: //kebab
                    richTextBox_notes1.Text = "T125B32R32B32R32A#32R32B32R32A#32R32B32R16.G#32R32G#32R32A32R32A32R32A32R16.A32R32A32R32A32R16.G32R32G32R32G32R32F#32R32E32R32E32R32G32R32E32R32F#32R32F#32R32F#32R32<B32R32>G#32A32G#32R32A#32R32B32R32B32R32A#32R32B32R32A#32R32B32R16.G#32R32G#32R32A32R32A32R32A32R16.A32R32A32R32A32R16.G32R32G32R32F#32R32F#32R32E32R32E32R32G32R32E32R32F#4.F#16<B32R32B32R16.B32R32B32R32>C#32R32C#32R32D#32R16.>E32R32D#32R32E32R32D#32R32E32R16.C#32R32C#32R32D32R32D32R32D32R16.D32R32D32R32D32R16.C32R16.C32R32<B32R32A32R32A32R32>C32R32<A32R32B32R32B32R32B32R32E32R32>C#32D32C#32R32D#32R32E32R32E32R32D#32R32E32R32D#32R32E32R16.C#32R32C#32R32D32R32D32R32D32R16.D32R32D32R32D32R16.C32R32C32R32<B8A8>C32R32<A32R32B8.";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 16: //mario
                    richTextBox_notes1.Text = "T120<>E16E8E16R16C16E16R16G8R8<G8R8>C8R16<G8R16E8R16A8B8A#16A16R16G16.>E16G16.A8F16G16R16E8C16D16<B16R8>C8R16<G8R16E8R16A8B8A#16A16R16G16.>E16G16.A8F16G16R16E8C16D16<B16R8C16R16>G16F#16F16D#16R16E16R16<G#16A16>C16R16<A16>C16D16<C16R16>G16F#16F16D#16R16E16R16>C16R16C16C16R16<<G16R16C16R16>G16F#16F16D#16R16E16R16<G#16A16>C16R16<A16>C16D16R8D#16R8D16R8C16R8<G16G16R16C16R16C16R16>G16F#16F16D#16R16E16R16<G#16A16>C16R16<A16>C16D16<C16R16>G16F#16F16D#16R16E16R16>C16R16C16C16R16<<G16R16C16R16>G16F#16F16D#16R16E16R16<G#16A16>C16R16<A16>C16D16R8D#16R8D16R8C16R8<G16G16R16C16R16>C16C16R16C16R16C16D16R16E16C16R16<A16G16R16C16R16>C16C16R16C16R16C16D16E16<G16R8C16R8C16R16>C16C16R16C16R16C16D16R16E16C16R16<A16G16R16C16R16>E16E16R16E16R16C16E16R16G8R8<G8R8>C8R16<G8R16E8R16A8B8A#16A16R16G16.>E16G16.A8F16G16R16E8C16D16<B16R8>C8R16<G8R16E8R16A8B8A#16A16R16G16.>E16G16.A8F16G16R16E8C16D16<B16R8>E16C16R16<G8R16G#8A16>F16R16F16<A16R16C16R16B16.>A16A16.A16.G16F16.E16C16R16<A16G16R16C16R16>E16C16R16<G8R16G#8A16>F16R16F16<A16R16C16R16B16>F16R16F16F16.E16D16.C16<G16R16G16C8R8>E16C16R16<G8R16G#8A16>F16R16F16<A16R16C16R16B16.>A16A16.A16.G16F16.E16C16R16<A16G16R16C16R16>E16C16R16<G8R16G#8A16>F16R16F16<A16R16C16R16B16>F16R16F16F16.E16D16.C16<G16R16G16C8R8>C16C16R16C16R16C16D16R16E16C16R16<A16G16R16C16R16>C16C16R16C16R16C16D16E16<G16R8C16R8C16R16>C16C16R16C16R16C16D16R16E16C16R16<A16G16R16C16R16>E16E16R16E16R16C16E16R16G8R8<G8R8>E16C16R16<G8R16G#8A16>F16R16F16<A16R16C16R16B16.>A16A16.A16.G16F16.E16C16R16<A16G16R16C16R16>E16C16R16<G8R16G#8A16>F16R16F16<A16R16C16R16B16>F16R16F16F16.E16D16.C16<G16R16G16C8";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 17: //camplire stalker
                    richTextBox_notes1.Text = "T100<<B8>B8>D8F#8G#4F#8D8<<F#8>A8>C#8F#8G#4F#8C#8<D#8G#8>C8G#8A4G#8C8<<F#8>A8>C#8F#8G#8F#8E8<E8<B8>B8>D8F#8G#4F#8D8<<F#8>A8>C#8F#8G#4F#8C#8<D#8G#8>C8G#8A4G#8C8<<F#8>A8>C#8F#8G#8F#8E8<E8T130<B8>B8>D8F#8G#8F#8F#8D8<<F#8>A8>C#8G#8G#8F#8C#8<A8D#8G#8>C8G#8A8G#8G#8<G#8<F#8>A8>C#8F#8G#8F#8E8<E8<B8>B8>D8F#8G#8F#8D8<B8<F#8>A8>C#8G#8G#8F#8C#8<A8D#8G#8>C8G#16.G#32A4G#8<G#8<F#8>A8>C#8F#8G#2T80<<C#16.>G#16>C#16.E16.C#16<G#16.>E16.C#16<G#16.>E16.C#16<G#16.<F#16.>A16>C#16.E16.C#16<A16.>E16.C#16<A16.>E16.C#16<A16.<B16.>A16B16.>D#16.<B16A16.>D#16.<B16A16.>D#16.<B16A16.<E16.>G#16B16.>D#16.<B16G#16.<E16.>G#16B16.>D#16.<B16G#16.<A16.>G#16A16.>C#16.<A16G#16.<A16.>G#16A16.>C#16.<A16G#16.<D#16.>F#16A16.>C#16.<A16F#16.<D#16.>F#16A16.>C#16.<A16F#16.<D#16.>G16>C#16.D#16.C#16<G16.<D#16.>G16>C#16.D#16.C#16<F#16.<G#16.>F#16>C16.D#16.C16<F#16.<G#16.>F#16>C16.D#4<<C#16.>G#16>C#16.E16.C#16<G#16.>E16.C#16<G#16.>E16.C#16<G#16.>E16.<A16>C#16.E16.C#16<A16.>E16.C#16<A16.>E16.C#16<A16.>D#16.<A16B16.>D#16.<B16A16.>D#16.<B16A16.>D#16.<B16A16.B16.G#16B16.>D#16.<B16G#16.>D#16.<B16G#16.>D#16.<B16G#16.A16.G#16A16.>C#16.<A16G#16.>C#16.<A16G#16.>C#16.<A16E16.A16.F#16A16.>C#16.<A16F#16.>C#16.<A16F#16.>C#16.<G#16E16.<D#16.>G16>C#16.D#16.C#16<G16.>D#16.C#16<G16.>D#16.C#16<F#16.>C16.<F#16>C16.D#16.C16<F#16.<G#16.>C#16G#16.>D#4T100<<C#8C#8>>E8<G#8<F#8F#8>>E8<A8<B8B8>>D#8<A8<E8E8>G#8>D#8<<A8A8>>C#8<E8F#8<D#8>>C#8<G#8<D#8D#8>>D#8<F#8<G#4>>D#8<E8<C#8C#8>>E8<G#8<F#8F#8>>E8<E8<B8B8>>D#8<A8<E8E8>>D#8<G#8<A8A8>>C#8<E8F#8<D#8>>C#8<G#8>C#16.<G16>C#16.D#16.C#16<G16.<G#16.>F#16>C16.D#4";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 18: //drige
                    richTextBox_notes1.Text = "T140E8<B8G8>F#8<B8G8>G8<B8>B8<B8G8>G8<B8G8>F#8<B8>D8<G8D8>E8D8<G8>F#8D8G8E8<G8>F#8D8<G8>E8D8E8<B8G8>F#8<B8G8>G8<B8>B8<B8G8>G8<B8G8>F#8<B8>D8<G8D8>E8D8<G8>F#8D8G8E8<G8>F#8D8<G8>E8D8<<E8B8>C8F#8G8F#8C8F#8<E8B8>C8F#8G2R1R1R1R1R1R1R1R1R1R1R1R1R1R1R1R1R1R1<E8B8>F#8G8B8G8F#8B8<E8>G8B8>B8B8A32B32A16G8F#8D8<G8D8>E8D8<G8>F#8D8G8E8<G8>F#8D8<G8>E8D8<<E8B8>F#8G8B8G8F#8B8<E8>G8B8>B8B8A32B32A16G8F#8D8<G8D8>E8D8<G8>F#8D8G8E8<G8>F#8D8<G8>E8D8<<E8B8>C8F#8G8F#8C8F#8<E8B8>C8F#8G2R1R1R1R1R1R1R1R1R1R1R1R1R1R1R1R1>E8<B8G8>F#8<B8G8>G8<B8>B8<B8G8>G8<B8G8>F#8<B8>E8<B8G8>F#8<B8G8>G8<B8>B8<B8G8>G8<B8G8>F#8<B8>E8<B8G8>F#8<B8G8>G8<B8>B8<B8G8>G8<B8G8>F#8<B8>E8<B8G8>F#8<B8G8>G8<B8>B8<B8G8>G8<B8G8>F#8<B8";
                    richTextBox_notes2.Text = "T140<<E8R8R8R8R8R8R8R8E8R8R8R8R8R8R8R8>C8<R8R8R8R8R8R8R8>G8R8R8R8R8R8R8R8<E8R8R8R8R8R8R8R8E8R8R8R8R8R8R8R8>C8<R8R8R8R8R8R8R8>G8R1R1R2R4R8E8<B8>F#8G8>B8<G8F#8<B8>>A8G8F#8G8<G8E8>A8<G8>A8G8F#8G8<G8F#8>F#8G8<G8E8G8>C8<G8E8G8G8E8<B8>F#8G8>B8<G8F#8<B8>>>C8C8<<G8>>C8<<G8>>C8<<E8G8>B8<F#8G8>D8<G8>A8<F#8C8>A8G8F#8G8<G8E8G8G8E8<B8>F#8>B8B8<G8>B8A8<E8>G8<G8>C8<G8E8>A8<G8>A8G8F#8G8<G8F#8F#8C8G8E8G8>C8<G8E8E8G8E8<B8>F#8G8>B8<G8F#8<B8>>>C8<<E8G8>C8<G8E8E8G8>B8<F#8G8>D8<G8F#8F#8C8>>D8<<E8G8>>C8<<G8E8>B8<G8>>C8<<F#8G8>B8<G8F#8>A8<C8>B8<E8G8>C8<G8E8C8G8><R1R1R1R1R1R1R1R1R1R1>G8<B8>F#8G8B8G8B8B8>C8<<E8>B8A8<G8>F#8F#8G8A8<F#8G8>D8B8<F#8>B8<G8>G8<E8>A8B8E8C8<G8E8E8<B8>F#8G8>B8<G8>B8<<B8>>>C8<<E8>B8A8<G8>F#8F#8G8A8<F#8G8>D8B8<F#8>B8<G8>G8<E8>F#8E8R8C8<G8E8G8<B8>F#8G8>B8<G8>B8<<B8>>A8<F#8>G8F#8<G8>E8E8D8<G8>D8<G8>C8B8C8<G8E8>>C8<<E8>B8A8E8G8<A8>A8B8<<B8>E8G8>B8<G8>B8A8<F#8>G8<G8>F#8<G8>E8E8D8<G8>D8<G8>C8B8C8B8<E8>G8F#8<A8>E4B8>C8E8<<<E8R8R8R8R8R8R8R8E8R8R8R8R8R8R8R8>C8<R8R8R8R8R8R8R8>G8R8R8R8R8R8R8R8<E8R8R8R8R8R8R8R8E8R8R8R8R8R8R8R8>C8<R8R8R8R8R8R8R8>G8";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 19: // Гимн
                    richTextBox_notes1.Text = "T120>C2C8.R8.<G8>C8.R16<G8R16A16B8.R16E8E8A8.R16G8R16F16G8.R16C8C8D8.R16D8E8F8.R16F8G8A8.R16B8>C8D4.<G8>E8.R16D8R16C16D8.R16<B8G8>C8.R16<B8R16A16B8.R16E8E8A8.R16G8F8G8.R16C8C8>C8.R16<B8R16A16G4.R8>E4.R8D8C8<B8>C8D4D16R16<G8G4.R8>C4.R8<B8A8G8A8B4B16R16E8E4.R8>C8.R16<A8R16B16>C8.R16<A8R16B16>C8.R16<A8>C8F4.R8F4.R8E8D8C8D8E4E16R16C8C4.R8D4.R8C8<B8A8B8>C4C16R16<A8A4.R8>C8.R16<B8A8G8.R16C8R16C16G4.R8A8.R16B8.R16>C2C8.";
                    richTextBox_notes2.Text = "T120<C2C8.R4R16C8.R16<C8.R16>E8.R16<E8.R16>F8.R16<F8.R16E8.R16E8.R16>D8.R16<D8.R16>D8.R16D8.R16<C8>C8B8<A8<G8.R16>G8.R16C8.R16E8.R16G8.R16<G8.R16A8.R16>C8.R16E8.R16E8.R16F8.R16D8.R16E8.R16E8.R16D8.R16D8.R16B8G8>C8<B8C4.R8>B8A8G8A8<G4G16R16G16R32G16R16.G8>F8<E8A4.R8>B8<A8>E8F8<E4E16R16E16R32E16R16.E8>D8<C8F4F16R16>C8A4A16R16G8>F4F16R16<C8A8G8F8E8<D4D16R16E16F16G8.R16<G8.R16>C8<B8>C8E8D8C8<B8A8>B4B16R16>C16D16E8.R16<E8.R16<A8B8>C8>E8<A8.R16G8.R16F8.R16D8.R16E8.R16E8.R16>B4.R8<G8.R16G8.R16>>C2C8.";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 20: // amogus
                    richTextBox_notes1.Text = "T125<C16R8.>>C8D#8F8F#8F8D#8C8R4<A#16>D16C8R4<<<G16R16>C16R8.>>C8D#8F8F#8F8D#8F#8R4.F#16.F16D#16.F#16.F16D#16.<<C16R8.>>C8D#8F8F#8F8D#8C8R4<A#16>D16C8R4<<<G16R16>C16R8.>>C8D#8F8F#8F8D#8F#8R4.F#16.F16D#16.F#16.F16D#16.<<C16";
                    richTextBox_notes2.Text = "T125C16R8.>>C8D#8F8F#8F8D#8C8R4<A#16>D16C8R4<<<G16R16>C16R8.>>C8D#8F8F#8F8D#8F#8R4.F#16.F16D#16.F#16.F16D#16.<<C16R8.>>C8D#8F8F#8F8D#8C8R4<A#16>D16C8R4<<<G16R16>C16R8.>>C8D#8F8F#8F8D#8F#8R4.F#16.F16D#16.F#16.F16D#16.<<C16\r\n";
                    comboBox_algorytm.SelectedIndex = 0;
                    break;
                case 21: //kalinka
                    richTextBox_notes1.Text = "T120R2.>E4D4<B8>C8D4<B8>C8D4C8<B8A4>E8E8D8.C16<B8>C8D4<B8>C8D4C8<B8A4>E8E8D4<B8>C8D4<B8>C8D4C8<B8A4>E8E8D8.C16<B8>C8D4<B8>C8D4C8<B8A4>E8E8<A4G4>E8G8F8E16D16C4<G4>E8G8F8E16D16C4<G4A4A8B8>D8C8<B8A8G4G4G4>G4E8G8F8E16D16C4<G4>E8G8F8E16D16C4<G4A4A8B8>D8C8<B8A8>G4F4E2R4E4D4<B8>C8D4<B8>C8D4C8<B8A4>E8E8D8.C16<B8>C8D4<B8>C8D4C8<B8A4>E8E8D4<B8>C8D4<B8>C8D4C8<B8A4>E8E8D8.C16<B8>C8D4<B8>C8D4C8<B8A8R8A4";
                    richTextBox_notes2.Text = "T120R1<<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<A8>A8<A8>A8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<A8>A8<A8>A8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<A8>A8<A8>A8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<A8>A8<A8>A8<A4G4>C8>C8<<G8>G8C8>C8<<G8>G8C8>C8<<G8>G8C8>C8<<G8>G8<F8>F8<F8>F8<G8>G8<G8>G8C8>C8<<G8>>C8<<G8>G8<G4>C8>C8<<G8>G8C8>C8<<G8>G8C8>C8<<G8>G8C8>C8<<G8>G8<F8>F8<F8>F8<G8>G8<G8>G8<G4D4E2R2E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<A8>A8<A8>A8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<A8>A8<A8>A8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<A8>A8<A8>A8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<E8>E8<A8E8A4";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 22: // bad piggies
                    richTextBox_notes1.Text = "T200D1R1R1>C4C8.<A#16>C16R16<A#16R16G#16R16G16R16A#16G#16G16F16D#16R16F16R16G2A#4A#8.G#16G16R16F16R16D#16R16F16R16G16D#16C16D#16G16R16F16R16D#4D4>C4C8.<A#16>C16R16<A#16R16G#16R16G16R16A#16G#16G16F16D#16R16F16R16G2A#4A#8.G#16G16R16F16R16D#16R16F16R16G16D#16C16D#16G16R16F16R16D#4D4R8C16D16D#16R16C16R16F8D#16R16D16R16C16R16<A#8.G#16G16R16G#8A#4F4R8D16D#16F16R16D16R16G8F16R16D#16R16D16R16D#4D#4D2R8>C16D16D#16R16C16R16F8D#16R16D16R16C16R16<A#16G#16G16G#16A#16R16G#16R16G4D#4R8D16D#16F16R16D16R16G8F16R16D#16R16D16R16D16R16D16D#16D16R16D16D#16D16R16D16D#16D16R16D16D#16D16D#16D16D#16D16D#16D16D#16D16D#16D16D#16D16D#16D16D#16G16G#16G16G#16G16G#16G16G#16G16A16B16>C16D16D#16F16G16B16R8.>C8.<A#16>C16R16<A#16R16G#16R16G16R16A#16G#16G16F16D#16R16F16R16G2A16R8.A#8.G#16G16R16F16R16D#16R16F16R16G16D#16C16D#16G16R16F16R16D#4D8C16G16>C4C8.<A#16>C16R16<A#16R16G#16R16G16R16A#16G#16G16F16D#16R16F16R16G2A#4A#8.G#16G16R16F16R16D#16R16F16R16G16D#16C16D#16G16R16F16R16D#4D4R8C16D16D#16R16C16R16F8D#16R16D16R16C16R16<A#8.G#16G16R16G#8A#4F4R8D16D#16F16R16D16R16G8F16R16D#16R16D16R16D#4D#4D8C16D16D#16G16>C16D16D#16R16C16D16D#16R16C16R16F8D#16R16D16R16C16R16<A#16G#16G16G#16A#16R16G#16R16G4D#4R8D16D#16F16R16D16R16G8F16R16D#16R16D16R16G16G#16G16G#16G16G#16G16G#16G16A16B16>C16D16D#16F16G16";
                    richTextBox_notes2.Text = "T200<D2D8<G16R16A16R16B16R16>C16R16D#16R16<G16R16>D#16R16C16R16D#16R16<G16R16>D#16R16C16R16D#16R16<G16R16>D#16R16C16R16D#16R16<G16R16>D#16R16C16R16D#16R16<G16R16>D#16R16C16R16D#16R16<G16R16>D#16R16D#16R16G16R16<A#16R16>G16R16D#16R16G16R16<A#16R16>G16R16<A#16R16>D16R16<F16R16>D16R16<A#16R16>D16R16<F16R16>D16R16<G#16R16>C16R16<D#16R16>C16R16<G16R16B16R16D16R16B16R16>C16R16D#16R16<G16R16>D#16R16C16R16D#16R16<G16R16>D#16R16D#16R16G16R16<A#16R16>G16R16D#16R16G16R16<A#16R16>G16R16<A#16R16>D16R16<F16R16>D16R16<A#16R16>D16R16<F16R16>D16R16<G#16R16>C16R16<D#16R16>C16R16<G16R16B16R16D16R16B16R16G#16R16>C16R16<D#16R16>C16R16<G#16R16>C16R16<D#16R16>C16R16F16R16G#16R16C16R16G#16R16F16R16G#16R16C16R16G#16R16<A#16R16>D16R16<F16R16>D16R16<A#16R16>D16R16<F16R16>D16R16<G#16R16>C16R16<D#16R16>C16R16<G16R16B16R16D16R16B16R16G#16R16>C16R16<D#16R16>C16R16<G#16R16>C16R16<D#16R16>C16R16F16R16G#16R16C16R16G#16R16F16R16G#16R16C16R16G#16R16<A#16R16>D16R16<F16R16>D16R16<A#16R16>D16R16<F16R16>D16R16<G4G#4G4G#4G8G#8G8G#8G8G#8G8G#8G8G#8G8G#8G8G8G8G8>C16R16D#16R16<G16R16>D#16R16C16R16D#16R16<G16R16>D#16R16D#16R16G16R16<A#16R16>G16R16D#16R16G16R16<A#16R16>G16R16<A#16R16>D16R16<F16R16>D16R16<A#16R16>D16R16<F16R16>D16R16<G#16R16>C16R16<D#16R16>C16R16<G16R16B16R16D16R16B16R16>C16R16D#16R16<G16R16>D#16R16C16R16D#16R16<G16R16>D#16R16D#16R16G16R16<A#16R16>G16R16D#16R16G16R16<A#16R16>G16R16<A#16R16>D16R16<F16R16>D16R16<A#16R16>D16R16<F16R16>D16R16<G#16R16>C16R16<D#16R16>C16R16<G16R16B16R16D16R16B16R16G#16R16>C16R16<D#16R16>C16R16<G#16R16>C16R16<D#16R16>C16R16F16R16G#16R16C16R16G#16R16F16R16G#16R16C16R16G#16R16<A#16R16>D16R16<F16R16>D16R16<A#16R16>D16R16<F16R16>D16R16<G#16R16>C16R16<D#16R16>C16R16<G16R16B16R16D16R16B16R16G#16R16>C16R16<D#16R16>C16R16<G#16R16>C16R16<D#16R16>C16R16F16R16G#16R16C16R16G#16R16F16R16G#16R16C16R16G#16R16<A#16R16>D16R16<F16R16>D16R16<A#16R16>D16R16<F16R16>D16R16<G#16R16>C16R16<D#16R16>C16R16<G16R16B16R16D16R16B16R16";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 23: // running into 90s
                    richTextBox_notes1.Text = "T300>C8<B8<<A4>>>C8<B8<<A4>>>C8<B8A#8A8G#8A8A#8B8>C8<B8<<A4>>>C8<B8<<A4>>>C8<B8A#8A8G#8A8A#8B8>C8<B8<<A4>>>C8<B8<<A4>>>C8<B8A#8A8G#8A8A#8B8>C8<B8<<A4>>>C8<B8<<A4>>>C8<B8A#8A8G#8A8A#8B8A8A16>E16D16C16<G8A8A16>E16D16C16<G8>C16<B16G16>C8<B16G8>C16<B16G16>D8<B16G8A8A16>E16D16C16<G8A8A16>E16D16C16<G8>C2C4<B4A8A16>E16D16C16<G8A8A16>E16D16C16<G8>C16<B16G16>C8<B16G8>C16<B16G16>D8<B16G8A8A16>E16D16C16<G8A8A16>E16D16C16<G8>C8<B8A#8A8G#8A8A#8B8A16E16A16>E16D16C16<G8A16E16A16>E16D16C16<G8>C16<B16G16>C8<B16G8>C16<B16G16>D8<B16G8A16E16A16>E16D16C16<G8A16E16A16>E16D16C16<G8A16A8A16B8B8>C16C8C16<B8B8A16E16A16>E16D16C16<G8A16E16A16>E16D16C16<G8>C16<B16G16>C8<B16G8>C16<B16G16>D8<B16G8A16E16A16>E16D16C16<G8A16E16A16>E16D16C16<G8";
                    richTextBox_notes2.Text = "T300C8<B8<<A4>>>C8<B8<<A4>>>C8<B8A#8A8G#8A8A#8B8>C8<B8<<A4>>>C8<B8<<A4>>>C8<B8A#8A8G#8A8A#8B8>C8<B8<<A4>>>C8<B8<<A4>>>C8<B8A#8A8G#8A8A#8B8>C8<B8<<A4>>>C8<B8<<A4>>>C8<B8A#8A8G#8A8A#8B8A8A16>E16D16C16<G8A8A16>E16D16C16<G8>C16<B16G16>C8<B16G8>C16<B16G16>D8<B16G8A8A16>E16D16C16<G8A8A16>E16D16C16<G8>C2C4<B4A8A16>E16D16C16<G8A8A16>E16D16C16<G8>C16<B16G16>C8<B16G8>C16<B16G16>D8<B16G8A8A16>E16D16C16<G8A8A16>E16D16C16<G8>C8<B8A#8A8G#8A8A#8B8A16E16A16>E16D16C16<G8A16E16A16>E16D16C16<G8>C16<B16G16>C8<B16G8>C16<B16G16>D8<B16G8A16E16A16>E16D16C16<G8A16E16A16>E16D16C16<G8A16A8A16B8B8>C16C8C16<B8B8A16E16A16>E16D16C16<G8A16E16A16>E16D16C16<G8>C16<B16G16>C8<B16G8>C16<B16G16>D8<B16G8A16E16A16>E16D16C16<G8A16E16A16>E16D16C16<G8";
                    comboBox_algorytm.SelectedIndex = 0;
                    break;
                case 24: // polish cow
                    richTextBox_notes1.Text = "T180E16R16E16R16A16R16A16R16E16R16E16R16C16R4R16E16R8.E16R16E16R8.E16R4R16D16R8.D16R16D16R8.D16R4R16E16R16A16R16A16R16E16R16E16R16C16R8.E16R8.A16R16A16R16E16R16E16R16C16R8.C16R16C16R16E16R16E16R16E16R16E16R8.E16R8.E16R16D16R16D16R16D16R16D16R16D16R4R16E16R16A16R16A16R16E16R16E16R16C16R8.E16R16E16R16A16R16A16R16E16R16E16R16C16R4R16E16R8.E16R16E16R8.E16R4R16D16R8.D16R16D16R8.D16R4R16E16R16A16R16A16R16E16R16E16R16C16R8.E16R8.A16R16A16R16E16R16E16R16C16R8.C16R16C16R16E16R16E16R16E16R16E16R8.E16R8.E16R16D16R16D16R16D16R16D16R16D16R4R16E16R16A16R16A16R16E16R16E16R16C16";
                    richTextBox_notes2.Text = "T180<E16R16E16R16A16R16A16R16E16R16E16R16C16R4R16E16R8.E16R16E16R8.E16R4R16D16R8.D16R16D16R8.D16R4R16E16R16A16R16A16R16E16R16E16R16C16R8.E16R8.A16R16A16R16E16R16E16R16C16R8.C16R16C16R16E16R16E16R16E16R16E16R8.E16R8.E16R16D16R16D16R16D16R16D16R16D16R4R16E16R16A16R16A16R16E16R16E16R16C16R8.E16R16E16R16A16R16A16R16E16R16E16R16C16R4R16E16R8.E16R16E16R8.E16R4R16D16R8.D16R16D16R8.D16R4R16E16R16A16R16A16R16E16R16E16R16C16R8.E16R8.A16R16A16R16E16R16E16R16C16R8.C16R16C16R16E16R16E16R16E16R16E16R8.E16R8.E16R16D16R16D16R16D16R16D16R16D16R4R16E16R16A16R16A16R16E16R16E16R16C16";
                    comboBox_algorytm.SelectedIndex = 0;
                    break;
                case 25: //undertale home
                    richTextBox_notes1.Text = "T120<<A16R16>E16A16R16B16R8E16A16R16B16C#16R16E16A16R16>E16R8<E16A16R16B16C16R16E16A16R16B16R8E16A16R16B16R8E16A16R16>E16R4.<<A16R16>E16A16R16B16R8E16A16R16B16C#16R16E16A16R16>E16R8<E16A16R16B16D16R16F#16A16R16>E16R8<F#16A16R16B16R8>A16G#16R16E16R8<A16B16R16A16<A16R16>E16A16R16B16R8E16A16R16B16C#16R16E16A16R16B16R8E16A16R16B16C16R16E16A16R16B16R8>E16<B16R16A16C16R16E16A16R16B16R8>E16<E16R16A16<A16R16>E16A16R16B16R8E16A16R16>E16<C#16R16E16A16R16>E16R8D16C#16R16<A16C16R16E16A16R16B16R8E16A16R16B16<B16R16>A16R16B16R8E16.>E16R2.R8R32E16R16<E16A16R16>>E16R8<<A16E16R8>B16R16<G#16B16R16>E16R8<B16G#16R8C16R8G16R16>>E16R8<<G16C16R16<B16R8>F#16A16R16B16E16R16<E16B16R16>E16>E16R16<E16A16R8>A16R8<E16R8>>E16R8<<G#16R16>>F#16R8<<G#16B16R8>>E16R8<<G16R8>B16R16<A16R8>A16R8<E16A16R16>C#16<B16R16A16E16R8>E16R16<E16A16R16>>E16R8<<A16E16R8>B16R16<G#16B16R16>E16R8<B16G#16R16C#16C16R16G16>C16R8>E16R16<<G16C16R16<B16R8>F#16A16R16B16R8<E16>E16R8>E16R16<E16A16R8>A16R8<E16R8>>E16R8<<G#16R16>>F#16R8<<G#16B16R8>>E16R8<<G16R8>B16R16<A16R8>A16R8<E16A16R16>C#16<B16R16A16E16R8>E16R8<A16R8>>E16R8C#16R8<B16R8A16R8G#16R16A16<A16R8C#16R8A16R8>>G#16R16G16<<A16>>F#16R16E16R8<<A16R8C16R8A16R8>F#16R4R16>E16R8C#16R8<B16R8A16R8B16R8>C#16R8<G#16R4R16B16R4R16B16R8A16R8G#16R8A16R8E16R8<A16R8>>E16R8C#16R8<B16R8A16R8G#16R16A16<A16R8>B16R8<A16R8>>G#16R16G16<<A16>>F#16R16E16R8<<A16R8C16R8A16R8>F#16R4R16>E16R8C#16R8<B16R8A16R8B16R8>C#16R8<G#16R4R16B16R4R16B16R8A16R8G#16R8A16R8E16R1R16D16R8E16R8C#16R2R8.<B16R8>C#16R8<A16R2.R16C#16R1R16A16R2R16<B16R4.R16>E16";
                    richTextBox_notes2.Text = "";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 26: // erika
                    richTextBox_notes1.Text = "T300<A#2.B4>C#2C#2C#2F#2F#2A#2A#2.G#4F#4R4<C#4R4<A#4R4>C#4R4>F2F#2G#4R4<<F4R4>C#4R4C#4R4>A#2.G#4F#4R4<<F4R4D#4R4C#4R4>A#2.B4>C#2C#2C#2F#2F#2A#2A#2.G#4F#4R4<C#4R4<A#4R4>C#4R4>F2F#2G#4R4<<F4R4>C#4R4C#4R4>A#2.G#4F#4R4<F4R4D#4R4C#4R4>C#2.F#4F2F2F2F2D#2F2F#4C#8C#8C#4F#2";
                    richTextBox_notes2.Text = "T300A#2.B4>C#2C#2C#2F#2F#2A#2A#2.G#4F#4R4<C#4R4<A#4R4>C#4R4>F2F#2G#4R4<<F4R4>C#4R4C#4R4>A#2.G#4F#4R4<<F4R4D#4R4C#4R4>A#2.B4>C#2C#2C#2F#2F#2A#2A#2.G#4F#4R4<C#4R4<A#4R4>C#4R4>F2F#2G#4R4<<F4R4>C#4R4C#4R4>A#2.G#4F#4R4<F4R4D#4R4C#4R4>C#2.F#4F2F2F2F2D#2F2F#4C#8C#8C#4F#2";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 27: // minecraft revenge
                    richTextBox_notes1.Text = "T175<R1R1R1R1R8.>D16R16D16R16D16R16D16R16C16R16C8.R16C16R16C16R16C16R16C16R16C16R16D16R16<A#8.R16G8.R16A#8.R16>D4R8<G8R8G8A#8R8>D4R4.D16R16D16R16D16R16D16R16C16R16C8.R16C16R16C16R16C16R16C16R16C16R16D16R16<A#8.R16G8.R16A#8.R16>D4R8<G8R8G8A#8R8>D4R4D16R4R16<A#4R4A#8>C16R16C16R16C16R8.C16R16C16R16D16R16D#16R16D4R8<A#8.R4.R16>C16R16C16R16C16R8.C16R16C16R16<A#8A8.R16>F8F16R16F16R16F16R16D#16R16D8.R16C16R16C16R16C16R16C16R16C16R16D8<A#8.R16G8.R16A#8.R16>D4R8<G8R8G8A#8>D16R16G16R16G16R16A16R16A#2.A#16R16D16R16A#16R8.A#16R16A16R16A16R16G16R16F16R16F16R16F16R8.G16R16G2G16R2R16D16R16G16R16G16R16A16R16A#2.A#16R16D16R16A#16R8.A#16R16A16R8.G16R16F16R16F16R16F16R8.G16R16G2G16R1R16<A#16R16>C16R8.C16R8.C16R8C16R8<A#16R16>C16R8.C16R8.C16R8C16R8<A#16R16>C16R8.C16R8.C16R8D16R8<A#16R16A#16R16G16R8.>D16R16G16R16G16R16A16R16A#2.A#16R16D16R16A#16R8.A#16R16A16R16A16R16G16R16F16R16F16R16F16R8.G16R16G2G16";
                    richTextBox_notes2.Text = "T175<R16<G16R8.G16R8.A#16R8A#16R8A#16R16F16R8.F16R8.A16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.A#16R8A#16R8A#16R16F16R8.F16R8.A16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.A#16R8A#16R8A#16R16F16R8.F16R8.A16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.A#16R8A#16R8A#16R16F16R8.F16R8.A16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.A#16R8A#16R8A#16R16F16R8.F16R8.A16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.A#16R8A#16R8A#16R16F16R8.F16R8.A16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.A#16R8A#16R8A#16R16F16R8.F16R8.A16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.A#16R8A#16R8A#16R16F16R8.F16R8.A16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.A#16R8A#16R8A#16R16F16R8.F16R8.A16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16R16G16R8.G16R8.G16R8A#16R8A#16";
                    comboBox_algorytm.SelectedIndex = 1;
                    break;
                case 28: // любэ опера
                    richTextBox_notes1.Text = "T175R2R8R32E32R16.F#32R16.G32R16F#8R16E16F#32R16.G32R16.F#16.R32E32R16.F#32R16.G32R16.B2R8E32R16.F#32R16.G32R16.F#16.R16.E16F#32R16.G32R16.F#16.R32E16R16F#32R16.G32R16.>C2R8<A32R16.B32R16.>C32R16.<B8R16A32B16R16.>C32R16.<B16.R32A32R16.G32R16.F#32R16.A8R16G32R32A32R16.B32R16.A16.R32G16R16F#32R16.E32R16.G8R16F#32R32G32R16.A32R16.G16.R32F#32R16.E32R16.D#32R16.E4.E16.R8.E32R16.F#32R16.G32R16F#8R16E16F#32R16.G32R16.F#16.R32E32R16.F#32R16.G32R16.B2R8E32R16.F#32R16.G32R16.F#16.R16.E16F#32R16.G32R16.F#16.R32E16R16F#32R16.G32R16.>C2R8<A32R16.B32R16.>C32R16.<B8R16A32B16R16.>C32R16.<B16.R32A32R16.G32R16.F#32R16.A8R16G32R32A32R16.B32R16.A16.R32G16R16F#32R16.E32R16.G8R16F#32R32G32R16.A32R16.G16.R32F#32R16.E32R16.D#32R16.E4R4E1E16R16E16R16F#32R16.G32R16.A8A32R32G32R32F#32R16.E32R16.D8D32R32C32R32<B16R16>C32R16.D4.D16R16C1C32R16.C32R16C32R8C32R16.<B8R32A32R32G32R8A32R16.B2.B16R8.>E1E32R16E16R16.F#32R16.G32R16.A8A32R32G32R32F#32R16.E16R16D8D32R32C32R32<B32R16.>C32R16.D8.R16E16R8.E8C8C32R16.<A16R16B16R4R16B32R16.>E2E8.R16E8E32R16E8E32C16.R8R32C16R16<B8R16B32R32B32R16.B32R16.>E2E16";
                    richTextBox_notes2.Text = "T175<<R1E8E32R16.G8G32R16.A8A32R16.B8B32R16.E8.R16G8G32R16.A8A32R32A32R32B8.R16E8E32R16.G8G32R16.A8A32R16.B8B32R16.A8A32R16.>C8C32R16.D8D32R16.E8E32R16.<A8A32R16.>C8C32R16.<D8D32R16.F#8F#32R16.G8G32R16.B8.R16>C8C32R16.E8.R16<F#8F#32R16.A8A32R16.B8B32R16.>D#8.R16<E8.R16G8G32R16.A8A32R32A32R32B8.R16E8E32R16.G8G32R16.A8A32R16.B8B32R16.E8.R16G8G32R16.A8A32R32A32R32B8.R16E8E32R16.G8G32R16.A8A32R16.B8B32R16.A8A32R16.>C8C32R16.D8D32R16.E8E32R16.<A8A32R16.>C8C32R16.<D8D32R16.F#8F#32R16.G8G32R16.B8.R16>C8C32R16.E8.R16<F#8F#32R16.A8A32R16.B8B32R16.>D#8.R16<E8.R4R16>C8.R16C8.R16<B8B32R16.B8.R16A8.R16A8A32R16.>D8.R16D8.R16<G8.R16G8.R16E8E32R16.G#8G#32R16.A8A32R16.A8.R16A8.R16A8A32R16.B8B32R16.B8B32R16.B8B32R16.B8B32R16.E8.R16G8G32R16.A8A32R32A32R32B8.R16>C8.R16C8.R16<B8B32R16.B8.R16A8.R16A8A32R16.>D8.R16D8.R16.<G8R16.G8G32R16.G8R8G#8G#32R16.A8A32R16.A8A32R16.B8B32R16.B8.R16E8E32R16.G8G32R16.B8B32R16.G8G32R16.A8.R16A8R8B8B32R16.B8B32R16.E8E32R2.";
                    comboBox_algorytm.SelectedIndex = 0;
                    break;



                /*
                richTextBox_notes1.Text = "";
                richTextBox_notes2.Text = "";
                comboBox_algorytm.SelectedIndex = 0;

                break;
                */
                default: break;

            }
        }

        #endregion

        #region debug



        private async void button_debug_Click(object sender, EventArgs e)
        {
            //собрать ноты
            //AssenblyPreparedNotes();
            //вывести инструкции в текстбокс 2
            //richTextBox_notes2.Text = string.Join("\n", instructionList_track1);



            //MessageBox.Show(algorytm.ToString());
            //comboBox_algorytm.SelectedIndex = 1;
            //richTextBox_notes1.Text = string.Join("\n", instructionList_track1);
            //richTextBox_notes2.Text = string.Join("\n", instructionList_track2);




            //richTextBox_notes1.Text = string.Join("\n", newTrack1);
            //richTextBox_notes2.Text = string.Join("\n", newTrack2);

            //richTextBox_notes1.Text = string.Join("\n", newTrack);
            //await PlayTrack(newTrack1);




            //// Создаем список для задач воспроизведения
            //List<Task> playTasks = new List<Task>();
            //playTasks.Add(test_play(newTrack1));
            //playTasks.Add(test_play(newTrack2));
            //await Task.WhenAll(playTasks);
            //
        }





        #endregion




       
    }
}
