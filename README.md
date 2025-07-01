# Tournament Runner
[TournamentRunner](https://github.com/PokerBotsBoras/TournamentRunner)

I detta repo finns koden som kör alla bottar mot varandra. Du kan se hur ofta de körs i [`.github/workflows/run-bots.yml`](.github/workflows/run-bots.yml).

Testerna ligger i `test/` och den faktiska koden i `src/`. Projektet heter `TournamentRunner`. Den mest intressanta delen finns i mappen [`Engine`](src/TournamentRunner/Engine), där du kan följa hur spelet fortgår under en pokerhand, samt hur `GameState`-objektet — som skickas till bottarna — byggs upp.

Turneringen körs en gång i timmen. Resultat visas på:
[pokerbotsboras.grgta.xyz/ratings.html](https://pokerbotsboras.grgta.xyz/ratings.html)
Ledartavlan bygger på ELO-rating ([Wikipedia](https://en.wikipedia.org/wiki/Elo_rating_system)).

---

## Teknisk översikt

### `Abstractions.cs`

Definierar datastrukturer för alla bots. Samma fil finns i template repot.
I början av filen finns `IPokerBot`-gränssnittet:

```csharp
public interface IPokerBot
{
    string Name { get; }
    PokerAction GetAction(GameState state);
}
```

En bot tar emot ett `GameState`-objekt och returnerar ett `PokerAction` (fold, call, eller raise).

---

### Du implementerar IPokerBot i ditt repo
I din egen bot (i ditt repo, baserat på BotTemplate) implementerar du gränssnittet IPokerBot. Turneringen kommer att använda din kod exakt som en ExternalPokerBot, vilket innebär att:

 - ✅ Den exekveras som en fristående process (Sköter TurnamentRunner)

 - ✅ Den får ett JSON-formaterat GameState via stdin (finns i redan i template)

 - ✅ Den måste svara med en JSON-formaterad PokerAction via stdout (detta är redan löst i templaten)

 - ⚠️ Den måste svara inom 1 sekund

 - ‼️ ⚠️ Det är **i din implementation av IPokerBot som all din spelstrategi ska sitta** ⚠️  ‼️.
 
 Implementationen av IpokerBot är egentligen det enda du behöver ändra ör att göra en komplett bot.

---

## GameState Objektet

```cs
public class GameState
{
    //Alltid *din* stack, hur många chips du har
    public int MyStack { get; set; }
    //Alltid motståndarens stack, hur många chips den har
    public int OpponentStack { get; set; }
    public int Pot { get; set; }
    //Det första kortet som man har från första början av handen
    public Card MyCard { get; set; } = null!;
    //Det andra kortet som man ser först efter flop
    public Card? CommunityCard { get; set; }
    //Hur mycket mer du behöver betta för att Call
    public int ToCall { get; set; }
    //Hur mycket mer du behöver höja utöver ToCall för att höja
    public int MinRaise { get; set; }
    //The first action belongs to the bot that had the SB, if there is 0 objects 
    // in this list when the bot gets this objects, you have smallblind, 
    // ActionHistory.Count % 2 == 0 is small blind
    // ActionHistory.Count % 2 == 1 is big blind
    public List<PokerAction> ActionHistory { get; set; } = new();
    // Detta resultat blir satt först efter en vinnare är utsedd i handen. 
    public HandResult? HandResult { get; set; }
}

//Detta objektet kommer en gång per hand sist, och visar det totala resultatet av handen.
public class HandResult : PokerEvent
{
    //Card of the bot that had the SB
    public Card SmallBlindBotCard { get; set; } = null!; 
    //Card of the bot that has the BB
    public Card BigBlindBotCard { get; set; } = null!;
    public Card CommunityCard { get; set; } = null!;
    public int Pot { get; set; }
    public string Winner { get; set; } = string.Empty; // or enum if preferred
    public bool IsTie { get; set; }
    // Optionally: public List<PokerAction> Actions { get; set; }
}
```

När din bot får detta objekt är MyStack alltid din stack. Men historiken ser lika dan ut för båda bottarna, du får utreda själv vilka PokerActions i historiken som är dina (de är i ordning).

---

### Hur PokerEngine använder dessa typer

**Exekvering av hand:**

```csharp
var state = BuildGameState(current, null, null);
var action = bots[current].GetAction(state);
actionHistory.Add(action);
```


Vid showdown:

```csharp
var rankSB = HandEvaluator.Evaluate(sbCard, community!);
var rankBB = HandEvaluator.Evaluate(bbCard, community!);
```

Resultat skickas i en `HandResult` till båda bottarna (deras svar används inte):

>**OBS** I slutet av varje hand, i metoden PlayHand, skickas ett GameState till båda bottarna med egenskapen HandResult ifylld. Detta innehåller bl.a. motståndarens kort. Turneringen kräver att botten svarar inom 1 sekund, men svaret används inte – det måste bara vara korrekt formaterat. *Du kan alltså returnera vilken giltig PokerAction som helst* vid det tillfället.

```csharp
var finalState = BuildGameState(i, community, result);
bots[i].GetAction(finalState);
```

### Raise
Om botten vill raise efter att motstandaren har gjort en raise, så måste alltså botten syna och raise: detta görs genom att skicak ett PokerAction med ActionType Raise, också kommer ToCall-summan att adderas
```
else if (action.ActionType == PokerActionType.Raise)
{
    int raiseAmount = action.Amount ?? minRaise;
    if (raiseAmount < minRaise && raiseAmount < stacks[current])
        raiseAmount = minRaise;
    int totalToPut = toCalls[current] + raiseAmount;
    // ...
``

---

## Extern botkommunikation

`ExternalPokerBot` startar botprocesser och pratar via JSON:

```csharp
_stdin.WriteLine("__name__");
_stdin.Flush();
Name = _stdout.ReadLine() ?? "UnknownBot";

string json = JsonSerializer.Serialize(state, _jsonOptions);
_stdin.WriteLine(json);
_stdin.Flush();

var readTask = _stdout.ReadLineAsync();
if (!readTask.Wait(1000))
    throw new BotException(Name, new TimeoutException(...));
```

Motsvarande kommando används för att återställa boten mellan rundor: `__reset__`.

---

## Implementera `IPokerBot` i detta repot

Exempel på enkla strategier finns i `RandomBot` och `SmartBot`. Dessa används med `InstanceResettablePokerBot` för att återställas mellan rundor. Du kan trycka in din egen implementation där om du klinar ner TournamentRunner repot för att köra det hur många gånger du vill.

> OBS!: Du kan behålla tillstånd (som vriabler i din PokerBot-klass) under en runda (upp till 100 händer), men inte mellan rundor.

---

## Använda `HandEvaluator`

Klassen `HandEvaluator` returnerar ett `HandRank` med t.ex. `Type`, `HighValue`, och `LowValue`. Du kan använda den direkt eller bygga en egen om du vill experimentera med annan strategi.

---

# Tips för strategi

* **Börja enkelt:** Implementera `GetAction` med grundläggande logik. T.ex. "alltid call om kortet är högt".
* **Utnyttja `GameState`:**

  * `ToCall` – hur mycket du måste syna.
  * `MinRaise` – minsta tillåtna höjning.
  * `CommunityCard` – för att utvärdera handen.
* **Använd `HandEvaluator`:** Snabb uppskattning av handstyrka.
* **Följ motståndaren:**
  `ActionHistory` innehåller hela bettinghistoriken. Använd den för att hitta mönster och anpassa din strategi.
  Eftersom boten inte omstartas förrän efter en runda (upp till 100 händer), kan det löna sig att hålla koll på motståndarens spelstil.
* **Ha koll på pott och stackar:** Tänk på risken innan du gör stora drag.
* **Undvik timeouts:** Svara inom \~1 sekund. Förberäkna om möjligt.

---

## Testa, lär, förbättra

Eftersom TournamentRunner körs varje timme, kan du:

1. Pusha en tag som börjar med `v*` för att submitta (se README i BotTemplate).
2. Se resultatet på ledartavlan.
3. Uppdatera boten och förbättra logiken.

---

