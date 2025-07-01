# Tournament Runner

I detta repo finns koden som kör alla bottar mot varandra. Du kan se hur ofta de körs i [`.github/workflows/run-bots.yml`](.github/workflows/run-bots.yml).

Testerna ligger i `test/` och den faktiska koden i `src/`. Projektet heter `TournamentRunner`. Den mest intressanta delen finns i mappen [`Engine`](src/TournamentRunner/Engine), där du kan följa hur spelet fortgår under en pokerhand, samt hur `GameState`-objektet — som skickas till bottarna — byggs upp.

En viktig detalj är att i slutet av varje hand, i metoden `PlayHand` i `PokerEngine`, skickas ett `GameState`-objekt till bottarna med `HandResult`-egenskapen satt. Det gör att bottarna får veta vad motståndaren hade för hand.

Vid det tillfället begärs ett svar från botten, men svaret används inte — det måste bara komma inom 1 sekund, annars diskvalificeras botten. Det går alltså att svara med vilken korrekt formaterad `PokerAction` som helst.
