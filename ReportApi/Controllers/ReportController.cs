namespace ReportApi.Controllers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    // Словарь с выполняющимися задачами
    private static readonly Dictionary<int, Task<Dictionary<string, ReportInfo>>> _tasks = new Dictionary<int, Task<Dictionary<string, ReportInfo>>>();
    
    /// <summary>
    /// Проверка статуса задачи
    /// </summary>
    /// <param name="taskId">Номер задачи</param>
    /// <returns></returns>
    [HttpGet("get-status")]
    public IActionResult GetStatus(int taskId)
    {
        // если нет ключа, то задача не была создана
        if (!_tasks.ContainsKey(taskId))
        {
            return NotFound("Задача не найдена!");
        }
        
        // задача еще выполняется
        if (_tasks[taskId].Status == TaskStatus.Running)
        {
            return Ok("Ваша задача еще выполняется, подождите");
        }
        
        // возвращаем результат задачи
        var result = _tasks[taskId].Result;
        _tasks.Remove(taskId);
        return Ok(result);
    }

    /// <summary>
    /// Получить номер задачи, которая предоставляет отчеты, когда будет выполнена
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="path"></param>
    /// <returns>Номер задачи</returns>
    [HttpPost("get-report")]
    public async Task<IActionResult> GetReport(string serviceName, string path)
    {
        if (!Directory.Exists(path))
        {
            return BadRequest("Пути к файлу с логами не существует");
        }
        
        var task = Task.Run(() => GetInformationAboutLogs(serviceName, path));
        _tasks.Add(task.Id, task);
        
        return Ok(task.Id);
    } 
    
    /// <summary>
    /// Получить информацию о логах
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="path"></param>
    /// <returns>Название сервиса и информация о нем</returns>
    private Dictionary<string, ReportInfo> GetInformationAboutLogs(string serviceName, string path)
    {
        if (path == null || path.Length == 0)
        {
            path = ".";
        }
        
        Dictionary<string, ReportInfo> reports = new Dictionary<string, ReportInfo>();

        // получаем регулярку переданную в аргументе метода
        Regex regex = new Regex(serviceName);
        
        //  получаем информацию о данной директории
        var direct = new DirectoryInfo(path);
        FileInfo[] directoryFiles = null;
        // проверка что из файла можно читать (т.е есть все разрешения)
        try
        {
            directoryFiles = direct.GetFiles();
        }
        catch
        {
            Console.WriteLine("Ошибка при чтении файла");
            return reports;
        }
        

        // получаем все файлы данной директории
        foreach (var file in directoryFiles)
        {
            // разделяем строку по символу - '.' и получаем имя сервиса (service_name)
            // если сервис не подходит по регулярке, то пропускаем данный лог
            if (!regex.IsMatch(file.Name.Split('.')[0]))
            {
                continue;
            }
            
            // получаем номер ротации
            var rotationNumber = file.Name.Split('.')[1];
            // получаем название сервиса
            var currentNameService = file.Name.Split('.')[0];
            
            // создаем сервис, если его до этого не было в данных (в нашем случае в словаре)
            if (!reports.ContainsKey(currentNameService))
            {
                reports[currentNameService] = new ReportInfo();
            }
            
            
            // получаем количество ротаций для определенного сервиса
            if (ulong.TryParse(rotationNumber, out ulong number))
            {
                reports[currentNameService].CountRotation = Math.Max(reports[currentNameService].CountRotation, number);
            }
            
            // обрабатываем логи для определенного сервиса
            ReadLogsInServiceFile(file.FullName, reports, currentNameService);
        }

        return reports;
    }

    /// <summary>
    /// чтение логов из файла для определенного сервиса
    /// </summary>
    /// <param name="fileFullName"></param>
    /// <param name="reports"></param>
    /// <param name="serviceName"></param>
    private void ReadLogsInServiceFile(string fileFullName, Dictionary<string, ReportInfo> reports, string serviceName)
    {
        // обработка ошибок при работе с файлами (например: закрыт доступ на чтение)
        try
        {
            // читаем по 1 строчки из определенного файла с логами
            using (StreamReader sr = System.IO.File.OpenText(fileFullName))
            {
                string line = String.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    var dateOfCurrentLine = FindDateInLogLine(line);
                    // Определяем являтся ли этот лог самым ранним
                    if (reports[serviceName].DateOfFirstLog > dateOfCurrentLine)
                    {
                        reports[serviceName].DateOfFirstLog = dateOfCurrentLine;
                    }

                    // Определяем являтся ли этот лог самым поздним
                    if (reports[serviceName].DateOfLastLog < dateOfCurrentLine)
                    {
                        reports[serviceName].DateOfLastLog = dateOfCurrentLine;
                    }

                    // находим имя текущей Severity и обновляем количество для этого типа Severity
                    // определенного сервиса
                    string nameSeverity = FindSeverityInLogLine(line);

                    if (!reports[serviceName].Severities.ContainsKey(nameSeverity))
                    {
                        reports[serviceName].Severities[nameSeverity] = 0;
                    }

                    reports[serviceName].Severities[nameSeverity]++;

                    // находим имя текущей Category и обновляем количество для этого типа Category
                    // определенного сервиса
                    string nameCategory = FindCategoryInLogLine(line);

                    if (!reports[serviceName].Categories.ContainsKey(nameCategory))
                    {
                        reports[serviceName].Categories[nameCategory] = 0;
                    }

                    reports[serviceName].Categories[nameCategory]++;

                    CountPercentagesSomeSection(reports, serviceName, "severity");
                    CountPercentagesSomeSection(reports, serviceName, "category");
                    // логируем одну строку из лог файла
                    LoadLog(line);
                }
            }
        } catch (Exception e)
        {
            // можно как-то залогировать что найден неправильный лог
            // в данном случае просто выводим информацию в консоль/можно сделать другую логику
            // этот кейс активируется, допустим, когда в логе почему-то пропущена строка
            Console.WriteLine(e.Message);
        }
    }
    
    // Максимальный размер лог файла
    private static long _Size = 1000;
    // Количество ротаций
    private static int _Rotation = 1;
    /// <summary>
    /// Сохраняем лог в директорию ./My_logs, под названием my_logs.<number>.log
    /// Каждый лог файл будет иметь по 1000 строчек логов 
    /// </summary>
    /// <param name="line"></param>
    private void LoadLog(string line)
    {
        // обработка ошибки если нельзя записывать в файл (например нет разрешений)
        try
        {
            var directory = new DirectoryInfo("./My_logs");
            if (!directory.Exists)
            {
                directory.Create();
            }

            var newAnonymEmail = CheckEmailInLine(line);

            if (_Size < 0)
            {
                _Rotation++;
                _Size = 1000;
            }

            string path = $"./My_logs/my_logs.{_Rotation}.log";
            System.IO.File.AppendAllText(path, newAnonymEmail + '\n');
            _Size--;
        }
        catch
        {
            Console.WriteLine("Ошибка при записи файл");
        }
    }

    /// <summary>
    /// Проверка что строка является email
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private string CheckEmailInLine(string line)
    {
        // регуляка для почты
        Regex regex = new Regex(@"([a-zA-Z0-9._-]+@[a-zA-Z0-9._-]+\.[a-zA-Z0-9_-]+)");
        if (regex.IsMatch(line))
        {
            var match = regex.Match(line);
            var anonymizeString = AnonymizeEmail(new StringBuilder(match.Value));
            // заменяем анонимизируем почту пользователя
            var temp = regex.Replace(line, anonymizeString);
            line = temp;
        }

        return line;
    }

    /// <summary>
    /// Анонимизация почты пользователя
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private string AnonymizeEmail(StringBuilder value)
    {
        for (int i = 0; i < value.Length; ++i)
        {
            if (value[i] == '@')
            {
                break;
            }
            if (i % 2 == 1)
            {
                value[i] = '*';
            }
        }
        return value.ToString();
    }

    /// <summary>
    /// Находим процентное соотношение для какой-то секции (Severity или Category)
    /// определенного сервиса
    /// </summary>
    /// <param name="reports"></param>
    /// <param name="serviceName"></param>
    /// <param name="nameSection"></param>
    private void CountPercentagesSomeSection(Dictionary<string, ReportInfo> reports,
                                             string serviceName,
                                             string nameSection)
    {
        // счетчик всех определенной секции одного сервиса
        ulong countAllSomeSection = 0;
        switch (nameSection)
        {
            // count for severities
            case "severity":
                foreach (var severity in reports[serviceName].Severities)
                {
                    countAllSomeSection += severity.Value;
                }
                
                if (countAllSomeSection == 0)
                {
                    return;
                }
                // пересчитываем для каждого процент
                foreach (var valueOfSection in reports[serviceName].Severities.Keys)
                {
                    reports[serviceName].PercentageOfEachSeverity[valueOfSection] 
                        = ((reports[serviceName].Severities[valueOfSection] / (1.0 * countAllSomeSection)) * 100);
                }
                break;
            // count for categories
            case "category":
                foreach (var category in reports[serviceName].Categories)
                {
                    countAllSomeSection += category.Value;
                }
                
                if (countAllSomeSection == 0)
                {
                    return;
                }
                // пересчитываем для каждого процент
                foreach (var valueOfSection in reports[serviceName].Categories.Keys)
                {
                    reports[serviceName].PercentageOfEachCategory[valueOfSection] 
                        = ((reports[serviceName].Categories[valueOfSection] / (1.0 * countAllSomeSection)) * 100);
                }
                break;
        }
    }

    /// <summary>
    /// Поиск имени Category в одном логе
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private string FindCategoryInLogLine(string line)
    {
        int indexOfThirdOpenBracket = 0;
        // ищем третью открывающуся скобку -> начало секции Category
        int countOpenBrackets = 0;
        for (int i = 0; i < line.Length; ++i)
        {
            if (line[i] == '[')
            {
                countOpenBrackets++;
            }

            if (countOpenBrackets == 3)
            {
                indexOfThirdOpenBracket = i;
                break;
            }
        }

        StringBuilder categoryString = new StringBuilder("");
        for (int i = indexOfThirdOpenBracket + 1; i < line.Length; ++i)
        {
            if (line[i] == ']')
            {
                break;
            }
            categoryString.Append(line[i]);
        }

        return categoryString.ToString();
    }

    /// <summary>
    /// Поиск имени Severity в одном логе
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private string FindSeverityInLogLine(string line)
    {
        // перескачили на 2 элемента, потому что возвращается индекс первой закрывающийся строки
        // а нас интересует только строка, находящаяся во вторых скобках
        int indexSecondOpenBracket = line.IndexOf(']') + 2;
        StringBuilder severityString = new StringBuilder("");

        while (line[indexSecondOpenBracket] != ']')
        {
            severityString.Append(line[indexSecondOpenBracket++]);
        }

        return severityString.ToString();
    }

    /// <summary>
    /// Поиск времени в одном логе
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private DateTime FindDateInLogLine(string line)
    {
        int indexOfFirstCloseBracket = line.IndexOf(']');
        StringBuilder stringDateTime = new StringBuilder("");
        for (int i = 1; i < indexOfFirstCloseBracket; ++i)
        {
            stringDateTime.Append(line[i]);
        }
        
        string format = "dd.MM.yyyy HH:mm:ss.fff";
        if (DateTime.TryParseExact(stringDateTime.ToString(), format,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime time)) {
            return time;
        }

        throw new ArgumentException("Неправильный шаблон лога");
    }
}