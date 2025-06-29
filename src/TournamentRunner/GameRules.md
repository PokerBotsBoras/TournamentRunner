
# ♠️ **Heads-Up Micro Hold’em – Official Rules**

---

## 🎯 **Overview**

**Heads-Up Micro Hold’em** is a minimalist, heads-up poker variant played with one private card per player and one shared community card. The goal is to accumulate more chips than your opponent by the end of a round through betting, bluffing, and hand strength.

* **Players**: 2
* **Deck**: Standard 52-card deck
* **Stack**: 1000 chips per player
* **Win Condition**: Opponent runs out of chips or you have the higher stack after 100 hands

---

## **Game Structure**

* The game consists of all bots competing one on one (heads-up) 
* Each matchup has 200 rounds
* The bots are ranked based on their calculated [Elo rating](https://en.wikipedia.org/wiki/Elo_rating_system) using the up to 200 wins and losses for each matchup.	

A **hand**: A single deal of cards with betting and (optionally) a showdown.
A **round**: Up to 100 hands between two bots, or until one cannot post blinds. where bots can strategize between hands (no resetting).
A **matchup**: All rounds between the same bot pair, the bots are reset between rounds.
A **tournament**: The full set of all matchups between all bot pairs

---

## 📐 **Round Structure**

* A **Round** is a sequence of up to 100 hands between two bots, or ends early if a player cannot post a blind.
* The player with the most chips at the end is the **round winner**, if chips are equal the result is a tie.
* Bots **do not retain memory** between rounds

---

## **Matchup between bot **

Every matchup of two bots, do a total of 200 rounds against eachother, 100 as SB and 100 BB

---

## 🔁 **Hand Flow**

Each hand follows these steps:

### 1. **Blinds Posted**

* Small Blind (SB): 10 chips
* Big Blind (BB): 20 chips
* SB acts first in all betting rounds
* **Blinds alternate** every hand

### 2. **Cards Dealt**

* Each player receives **1 private hole card**
* A **standard 52-card deck** is shuffled before each hand

### 3. **Preflop Betting**

* SB acts first: may **fold**, **call**, or **raise**
* Betting continues until one player folds, calls the last raise, or goes all-in and is called

### 4. **Community Card Reveal**

* A single **shared face-up community card** is dealt

### 5. **Postflop Betting**

* SB acts first again
* Same betting rules as preflop

### 6. **Showdown (if needed)**

* If no one folded, players reveal hands
* The stronger 2-card hand (hole + community) wins the pot
* Ties split the pot equally
* Both player hands are revealed

---

## 🏆 **Hand Rankings**

From strongest to weakest:

1. **Straight Flush** – Same suit and consecutive ranks
2. **Pair** – Same rank
3. **Straight** – Consecutive ranks, any suits
4. **Flush** – Same suit
5. **High Card** – None of the above

Note: pair is very strong, and change of flush and straight ranks, because of probability in 2 card poker.

---

## 🔢 **Card Value Reference**

| Rank | Value |
| ---- | ----- |
| 2–10 | 2–10  |
| J    | 11    |
| Q    | 12    |
| K    | 13    |
| A    | 14    | A is never worth 1

* **Aces are high only** A-2 is not a straight
* **Suits have no ranking** and do not affect hand strength or tie-breaking

---

## ⚖️ **Tie-Breaking Rules**

* If both players have the same hand type, the hand with the **higher relevant card value** wins
* If the values are also equal, the result is a **tie**, and the **pot is split evenly**
* The pot is always an even number in heads-up play; no rounding needed

---

## 💰 **Betting Rules**

### ✅ **Minimum Raise**

* To raise, the player must increase the current highest bet by at least the size of the previous raise amount. 

> example:
> player1 raises 20
> (player 2 can call, fold, or raise 20 or more, but not less)
> player2 raises 100
> (player1 can call, fold, or raise 100 or more, but not less)

* If no raise has occurred yet, the minimum raise is equal to the big blind (20 chips). This is treated as the baseline raise amount.
* All-in for less than the minimum is allowed.
* There is no maximum raise.

### 🔁 **Raise Limit**

* There is **no limit** on the number of raises per betting round

---

## 🧯 **All-In Rules**

### Functional Definition of **all-in**
> A player is considered **all-in** when they bets their entire stack **or as much as the opponent has in their stack**

* A player cannot bet more than they have
* A player may bet **more than the opponent has**, but excess chips are ignored
* **No side pots** are created in this game

**Example:**

* Player A: 1000 chips
* Player B: 300 chips

* A bets 300 (all-in) → B calls 300 → pot is 600 

or 

* A bets 1000 (all-in) → B calls 300 → pot is 600 → extra 700 from A is ignored

💰 All-In and Raise Limitation
If a player is all-in, their opponent may only call or fold.
No further raises are allowed once one player has committed their entire stack.

---

## 🚫 **Hand Eligibility & Round Termination**

A new hand cannot begin unless both players can post the required blinds:

* **SB must have at least 10 chips**
* **BB must have at least 20 chips**

If either player cannot post, the **round ends immediately**, and the player with more chips is declared the winner.

---

## 🧠 **Bot State & Memory**

* Bots are **reset before every Round of 100 hands**
* They **do not remember** past hands or outcomes between Rounds
* Between Hands **they can retain inner state**
