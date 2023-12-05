Задание выполнено со всеми требования, включая все звездочки.

Задание:
Реализовать сервис, который генерирует отчеты по логам.

API:
Имеет два эндпоинта:
Отправка запроса на сервис (POST) - /api/Report/get-report?serviceName=[..]&path=[..]
Получение статуса задачи (GET) - /api/Report/get-status?taskId=[taskId]

Примечание по коду:
Пункты 4-5 отчета храню в типе double (проценты определенной секции), потому что при хранении в uint/ulong/int и тд, теряются 1-3%, а может и больше из-за того, что происходит каст с целочисленному значению, поэтому было решено сохранять в типе double, чтобы иметь более верные проценты.

Требования со звездой:
1) Асинхронная реализация - методы GetReport обрабатывает запрос асинхронно, приэтом выдает пользователю id задачи, по которому он может обратиться и получить ответ задача выполнено/еще в процессе.

2) Метод GetStatus позволяет посмотреть статус задачи.

3) В задании не сказано куда логгировать информацию в систему поэтому просто написал, что в папку My_logs, в каждом лог файле будет находиться по 1000логов с анонимизацией персональных данный, сделал как указано в примере - (адресов электронной почты: example@domain.com -> e*a*p*e@domain.com)

Работу делал на Ubuntu(Linux) для проверяющий сгенерировал exe файлы под винду. В задании не было сказано про какое-то приложение (консольное), которое будет давать запросы в API, поэтому поставил http://localhost:5105/ для тестов в Swagger, если будете тестировать через консоль и например Postman/Insomnia, запуская исполняемый файл (не через IDE), то приложение переходит в режим production, меняется порт приложения (просто решил на всякий случай подметить :) )

Выполнил: Мирошниченко Денис @platina_777

Если приложение не хочет запускаться с данными exe, выполните команду dotnet build (создаться подходящий вашей ОС исполняемый файл).

Если есть желание оставить отзыв о работе, можно написать по этим контактам (хотелось бы просто знать, что делаю неправильно и дальше двигаться вперед):
тг - @platina_777
почта - miroshnichenkodenis2004@mail.ru

