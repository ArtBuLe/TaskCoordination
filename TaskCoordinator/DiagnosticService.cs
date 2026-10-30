using System.Diagnostics;
namespace TaskCoordinator;
public sealed class DiagnosticService
{
    
    public async Task<DiagnosticResult> RunAsync(string name, TimeSpan duration, CancellationToken token = default)
    {
        // TODO 1: Проверить имя и неотрицательную длительность.
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name),"Name can't be null");
        }
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "duration can't be lower than 0");
        }
        var sw = Stopwatch.StartNew();
        // TODO 2: Передать token в Task.Delay.
        await Task.Delay(duration, token);
        sw.Stop();
        // TODO 3: Вернуть статус Completed и длительность.
        return new(name, DiagnosticStatus.Completed, "PASS", sw.Elapsed);
    }
    public async Task<DiagnosticResult> RunCpuBoundAsync(string name, int iterations, CancellationToken token = default)
    {
        // TODO 4: Перенести CPU-bound цикл в Task.Run; регулярно проверять token.
        var sw = Stopwatch.StartNew();

        long sum = await Task.Run(() => 
        {
            long localsum = 0;
            for (int i = 0; i < iterations; i++)
            {
                if (i % 1000 == 0) token.ThrowIfCancellationRequested();
                localsum += i % 7;
            }
            return localsum;
        },token);

        sw.Stop();
        return new(name, DiagnosticStatus.Completed, $"Сумма: {sum}", sw.Elapsed);
    }
    public async Task<IReadOnlyList<DiagnosticResult>> RunAllAsync(CancellationToken token = default)
    {
        var tasks = new[] 
        { 
            RunAsync("Сеть", TimeSpan.FromMilliseconds(600), token), 
            RunAsync("Диск", TimeSpan.FromMilliseconds(900), token), 
            RunAsync("База", TimeSpan.FromMilliseconds(700), token)
        };
        // TODO 5: Дождаться всех задач через WhenAll.
        var result = await Task.WhenAll(tasks);
        return result;
    }
    public async Task<DiagnosticResult> RunWithTimeoutAsync(string name, TimeSpan duration, TimeSpan timeout, CancellationToken token = default)
    {
        // TODO 6: Создать linked CancellationTokenSource на основе внешнего token.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        // TODO 7: Настроить CancelAfter(timeout).
        cts.CancelAfter(timeout);
        // TODO 8: Отличить тайм-аут от отмены вызывающей стороны; тайм-аут сообщать TimeoutException.
        try
        {
            return await RunAsync(name,duration,cts.Token);
        }
        catch (OperationCanceledException ex)
        {
            if (token.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancelled", ex.CancellationToken);
            }
            throw new TimeoutException($"Operation '{name}' is timed out : {timeout}");
        }
    }
    public async Task<string> FirstFinishedAsync(CancellationToken token = default)
    {
        var tasks = new[] 
        {
            RunAsync("Быстрая", TimeSpan.FromMilliseconds(150), token),
            RunAsync("Средняя", TimeSpan.FromMilliseconds(350), token),
            RunAsync("Медленная", TimeSpan.FromMilliseconds(600), token)
        };
        // TODO 9: Использовать WhenAny, затем await первой задачи.
        var FirstFinished = await Task.WhenAny(tasks);
        var res = await FirstFinished;
        return res.Name;
    }
    // TODO 10: Добавить обёртку, которая возвращает статусы Failed/Cancelled
    // и текст ошибки вместо сокрытия исключений.
    public async Task<DiagnosticResult> RunSafeAsync(Func<Task<DiagnosticResult>> action, string name)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new DiagnosticResult(name, DiagnosticStatus.Cancelled, "Операция была отменена", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DiagnosticResult(name, DiagnosticStatus.Failed, $"{ex.GetType().Name}: {ex.Message}", sw.Elapsed);
        }
    }
}
