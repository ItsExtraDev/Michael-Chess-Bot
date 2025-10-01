using Michael.src.MoveGen;

//Inits
MatchManager.Init();
PrecomputeMoveData.Init();
Magic.Init();

/*
 * 8/8/1K4p1/P4kp1/8/7P/5PP1/8 w - - 1 54
 * [Event "My Tournament"]
[Site "?"]
[Date "2025.09.29"]
[Round "97"]
[White "MichaelV1.16b"]
[Black "MichaelV1"]
[Result "0-1"]
[ECO "A46"]
[GameDuration "00:00:25"]
[GameEndTime "2025-09-29T00:50:03.778 שעון קיץ ירושלים"]
[GameStartTime "2025-09-29T00:49:38.272 שעון קיץ ירושלים"]
[Opening "Queen's pawn game"]
[PlyCount "106"]
[Termination "abandoned"]
[TimeControl "30"]

1. d4 {book} Nf6 {book} 2. Nf3 {book} d5 {book} 3. c4 {book} e6 {book}
4. Nc3 {book} c6 {book} 5. Bg5 {book} Be7 {book} 6. e3 {book} Nbd7 {book}
7. Rc1 {book} O-O {book} 8. Bd3 {book} dxc4 {book} 9. Bxc4 {book} Nd5 {book}
10. Bxe7 {book} Qxe7 {book} 11. O-O {+0.21/8 0.76s} Qb4 {-0.42/4 0.11s}
12. Bb3 {+0.35/8 0.73s} Nxc3 {-0.42/4 0.11s} 13. Rxc3 {+0.47/7 0.72s}
Nf6 {-0.45/4 0.11s} 14. Qc2 {+0.58/7 0.70s} Nd5 {-0.48/4 0.11s}
15. Rc4 {+0.58/8 0.69s} Qb6 {-0.63/4 0.11s} 16. e4 {+0.68/8 0.67s}
Nf4 {-0.63/4 0.11s} 17. Ne5 {+0.73/7 0.66s} f5 {-0.64/4 0.11s}
18. exf5 {+0.91/7 0.64s} Rxf5 {-0.59/4 0.11s} 19. Rxc6 {+1.18/8 0.61s}
Ne2+ {-0.27/4 0.11s} 20. Qxe2 {+1.28/7 0.60s} bxc6 {-0.82/4 0.11s}
21. Qe4 {+1.23/7 0.58s} Rf6 {-0.82/4 0.11s} 22. Rc1 {+1.28/7 0.57s}
Qb5 {-0.92/3 0.10s} 23. Nxc6 {+2.28/7 0.57s} Qb7 {-0.89/3 0.11s}
24. d5 {+2.41/6 0.55s} Qf7 {-0.81/3 0.11s} 25. dxe6 {+2.80/7 0.53s}
Bxe6 {-1.94/4 0.11s} 26. Ne7+ {+2.75/7 0.52s} Qxe7 {-2.09/4 0.11s}
27. Qxa8+ {+2.80/7 0.51s} Kf7 {-2.50/5 0.11s} 28. Bxe6+ {+2.80/7 0.49s}
Rxe6 {-2.14/5 0.11s} 29. Qf3+ {+2.85/6 0.47s} Rf6 {-2.19/5 0.10s}
30. Qd5+ {+2.85/6 0.46s} Re6 {-2.24/4 0.11s} 31. Qh5+ {+2.85/6 0.46s}
Kf6 {-2.21/5 0.11s} 32. Qf3+ {+2.85/6 0.44s} Kg6 {-2.19/5 0.10s}
33. Qg4+ {+2.85/6 0.43s} Kf7 {-2.24/5 0.11s} 34. Qf5+ {+2.85/6 0.43s}
Qf6 {-2.24/5 0.11s} 35. Rc7+ {+2.90/6 0.41s} Re7 {-2.24/5 0.11s}
36. Qh5+ {+2.65/6 0.40s} Qg6 {-2.84/5 0.11s} 37. Qf3+ {+2.60/7 0.40s}
Qf6 {-2.24/5 0.11s} 38. Qh5+ {+2.43/7 0.38s} Qg6 {-2.84/5 0.11s}
39. Qxg6+ {+2.55/7 0.37s} hxg6 {-1.85/6 0.11s} 40. Rxe7+ {+2.55/10 0.37s}
Kxe7 {-2.05/8 0.10s} 41. Kf1 {+2.55/10 0.35s} Kf6 {-1.95/7 0.11s}
42. Ke2 {+2.65/11 0.35s} Ke5 {-2.05/7 0.10s} 43. Ke3 {+2.55/10 0.34s}
g5 {-2.10/8 0.11s} 44. b4 {+2.60/10 0.33s} Kd5 {-2.10/8 0.11s}
45. Kd3 {+2.60/10 0.32s} Ke5 {-2.15/7 0.10s} 46. h3 {+2.80/10 0.31s}
g6 {-2.20/7 0.11s} 47. Kc4 {+2.95/10 0.31s} Ke4 {-2.30/7 0.10s}
48. a4 {+3.00/10 0.29s} Ke5 {-2.45/8 0.10s} 49. b5 {+3.05/10 0.29s}
Ke4 {-2.40/8 0.11s} 50. a5 {+3.55/11 0.28s} Ke5 {-2.55/8 0.11s}
51. Kc5 {+6.10/11 0.27s} Ke6 {-2.90/8 0.11s} 52. b6 {+9.98/10 0.28s}
axb6+ {-6.96/8 0.11s} 53. Kxb6 {+9.95/9 0.26s}
Kf5 {-9.52/8 0.11s, White disconnects} 0-1


 */
UCI uci = new();
//Always listen UCI for command
//rom the GUI, and respond accordingly.
while (true)
{
    string message = Console.ReadLine();
    uci.ProcessCommand(message);
}