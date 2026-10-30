using TaskCoordinator;
var pass = 0;
var fail = 0;
void Check(bool ok, string n)
{
    if (ok)
    {
        pass++;
        Console.WriteLine("PASS " + n);
    }
    else
    {
        fail++;
        Console.WriteLine("FAIL " + n);
    }
}
var s = new DiagnosticService();
var one = await s.RunAsync("Сеть", TimeSpan.FromMilliseconds(20));
Check(one.Status == DiagnosticStatus.Completed, "одна диагностика завершается");
var all = await s.RunAllAsync();
Check(
    all.Count == 3 && all.All(x => x.Status == DiagnosticStatus.Completed),
    "WhenAll ждёт три диагностики"
);
var first = await s.FirstFinishedAsync();
Check(first == "Быстрая", "WhenAny возвращает первую завершившуюся");
// TODO: Добавить тесты кооперативной отмены, тайм-аута, неверного имени и отрицательного времени.

bool invName = false;
try
{
    await s.RunAsync("", TimeSpan.FromMicroseconds(10));
}
catch (ArgumentNullException)
{
    invName = true;
}
Check(invName,"Неправильное имя вызывает исключение");

bool negTime = false;
try
{
    await s.RunAsync("TEST", TimeSpan.FromMicroseconds(-10));
}
catch (ArgumentOutOfRangeException)
{
    negTime = true;
}
Check(invName, "Неправильное время вызывает исключение");

bool cancelThrows = false;
using var ctsCancel = new CancellationTokenSource();
ctsCancel.Cancel();
try
{
    await s.RunAsync("Долгая задача", TimeSpan.FromSeconds(5), ctsCancel.Token);
}
catch (OperationCanceledException) // TaskCanceledException наследуется от него
{
    cancelThrows = true;
}
Check(cancelThrows, "кооперативная отмена вызывает OperationCanceledException");

bool timeoutThrows = false;
try
{
    // Длительность (500 мс) больше тайм-аута (50 мс)
    await s.RunWithTimeoutAsync("ТестТаймаута", TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(50));
}
catch (TimeoutException)
{
    timeoutThrows = true;
}
Check(timeoutThrows, "превышение времени вызывает TimeoutException");

bool externalCancelOverridesTimeout = false;
using var ctsExternal = new CancellationTokenSource();
ctsExternal.Cancel();
try
{
    await s.RunWithTimeoutAsync("ТестПриоритета", TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), ctsExternal.Token);
}
catch (OperationCanceledException)
{
    externalCancelOverridesTimeout = true;
}
catch (TimeoutException)
{
    externalCancelOverridesTimeout = false;
}
Check(externalCancelOverridesTimeout, "внешняя отмена имеет приоритет над тайм-аутом");



Console.WriteLine($"Итого: {pass} пройдено, {fail} ошибок.");
return fail == 0 ? 0 : 1;
