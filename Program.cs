using System.Threading.Channels;

var random = new Random(args.Length == 0 ? Random.Shared.Next() : int.Parse(args[0]));

Console.Write("\x1b[?1049h\x1b[H");
PrintGreeting();

var keyReader = StartKeyConsumer();

uint score = 0;
uint combo = 1;
foreach (var (a, b) in GetSumSection(random))
{
    Console.WriteLine($"Score {score, 5} Combo {combo, 2}");
    Console.Write($"{a, 3} + {b, 3} =   ?");
    var sum = unchecked((byte)(a + b));
    switch (await GetInput(keyReader, sum == 0))
    {
        case Answer.Exit:
            PrintExit(score);
            return;
        case Answer.Timeout:
            Console.WriteLine($"\b\b\b\x1b[33m{sum, 3}\x1b[0m");
            score = score > 0 ? score - 1 : 0;
            combo = 1;
            break;
        case Answer.Correct:
            Console.WriteLine($"\b\b\b\x1b[32m{sum, 3}\x1b[0m");
            score += combo;
            combo++;
            break;
        case Answer.Wrong:
            Console.WriteLine($"\b\b\b\x1b[31m{sum, 3}\x1b[0m");
            score = score > 0 ? score - 1 : 0;
            combo = 1;
            break;
    }
}

PrintExit(score);

static void PrintGreeting()
{
    Console.WriteLine("Sum-0-ing");
    Console.WriteLine("Will the summation be zero?");
    Console.WriteLine("I'm poor and couldn't afford more than a byte to store the numbers.");
    Console.WriteLine();
}

static void PrintExit(uint score)
{
    Console.Write("\x1b[?1049l");
    PrintGreeting();
    Console.WriteLine($"Final score: {score}");
}

static ChannelReader<ConsoleKey> StartKeyConsumer()
{
    var channel = Channel.CreateBounded<ConsoleKey>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, }
    );
    _ = Task.Run(async () =>
    {
        while (true)
        {
            await channel.Writer.WriteAsync(Console.ReadKey(true).Key);
        }
    });
    return channel.Reader;
}

static async Task<Answer> GetInput(ChannelReader<ConsoleKey> keyReader, bool isAnswerZero)
{
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    try
    {
        while (true)
        {
            switch (await keyReader.ReadAsync(cts.Token))
            {
                case ConsoleKey.Escape:
                    return Answer.Exit;
                case ConsoleKey.LeftArrow when isAnswerZero:
                    return Answer.Wrong;
                case ConsoleKey.LeftArrow when !isAnswerZero:
                    return Answer.Correct;
                case ConsoleKey.RightArrow when isAnswerZero:
                    return Answer.Correct;
                case ConsoleKey.RightArrow when !isAnswerZero:
                    return Answer.Wrong;
            }
        }
    }
    catch (OperationCanceledException)
    {
        return Answer.Timeout;
    }
}

static IEnumerable<(byte, byte)> GetSumSection(Random random)
{
    (byte, byte)[] section =
    [
        GetZeroSum(random),
        GetZeroSum(random),
        GetZeroSum(random),
        GetZeroSum(random),
        GetZeroSum(random),
        GetZeroSum(random),
        GetNonZeroSum(random),
        GetNonZeroSum(random),
        GetNonZeroSum(random),
        GetNonZeroSum(random)
    ];
    random.Shuffle(section);
    return section;
}

static (byte, byte) GetZeroSum(Random random)
{
    byte a = (byte)random.Next(0, 256);
    byte b = (byte)(-a);
    return (a, b);
}

static (byte, byte) GetNonZeroSum(Random random)
{
    byte sum = (byte)random.Next(1, 256);
    byte a = (byte)random.Next(0, sum);
    byte b = (byte)(sum - a);
    return (a, b);
}

enum Answer
{
    Exit,
    Correct,
    Wrong,
    Timeout,
}
