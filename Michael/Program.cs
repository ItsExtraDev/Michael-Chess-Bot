using Michael.src.MoveGen;

//Inits
MatchManager.Init();
PrecomputeMoveData.Init();
Magic.Init();

/*
 * info depth 1 score cp 50 nodes 20 nps 2222 time 9 pv g1f3
info depth 2 score cp 0 nodes 110 nps 9166 time 12 pv g1f3 g8f6
info depth 3 score cp 50 nodes 778 nps 48625 time 16 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3397 nps 77204 time 44 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 22208 nps 122021 time 182 pv g1f3 g8f6 d2d4 b8c6 b1c3
info depth 6 score cp 0 nodes 124186 nps 343055 time 362 pv g1f3 g8f6 d2d4 b8c6 d4d5 c6b4
info depth 7 score cp 35 nodes 1072095 nps 692120 time 1549 pv e2e4 g8f6 e4e5 f6d5 b1c3 d5c3 d2c3
info depth 8 score cp 5 nodes 4969126 nps 740444 time 6711 pv b1c3 d7d5 e2e3 g8f6 f1b5 e6c8 b5c7 e6h3

info depth 1 score cp 50 nodes 20 nps 2500 time 8 pv g1f3
info depth 2 score cp 0 nodes 110 nps 10000 time 11 pv g1f3 g8f6
info depth 3 score cp 50 nodes 778 nps 48625 time 16 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3397 nps 80880 time 42 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 22208 nps 122021 time 182 pv g1f3 g8f6 d2d4 b8c6 b1c3
info depth 6 score cp 0 nodes 124186 nps 339306 time 366 pv g1f3 g8f6 d2d4 b8c6 d4d5 c6b4
info depth 7 score cp 35 nodes 1072095 nps 716641 time 1496 pv e2e4 g8f6 e4e5 f6d5 b1c3 d5c3 d2c3
info depth 8 score cp 5 nodes 4969126 nps 762486 time 6517 pv b1c3 d7d5 e2e3 g8f6 f1b5 e6c8 b5c7 e6h3

info depth 1 score cp 50 nodes 20 nps 2500 time 8 pv g1f3
info depth 2 score cp 0 nodes 110 nps 10000 time 11 pv g1f3 g8f6
info depth 3 score cp 50 nodes 778 nps 48625 time 16 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3397 nps 77204 time 44 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 22171 nps 121818 time 182 pv g1f3 g8f6 d2d4 b8c6 b1c3
info depth 6 score cp 0 nodes 123969 nps 336872 time 368 pv g1f3 g8f6 d2d4 b8c6 d4d5 c6b4
info depth 7 score cp 35 nodes 1055309 nps 707786 time 1491 pv e2e4 g8f6 e4e5 f6d5 b1c3 d5c3 d2c3
info depth 8 score cp 5 nodes 4793595 nps 756206 time 6339 pv b1c3 d7d5 e2e3 g8f6 f1b5 e6c8 b5c7 e6h3

info depth 1 score cp 50 nodes 20 nps 2500 time 8 pv g1f3
info depth 2 score cp 0 nodes 110 nps 11000 time 10 pv g1f3 g8f6
info depth 3 score cp 50 nodes 778 nps 55571 time 14 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3397 nps 130653 time 26 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 22171 nps 180252 time 123 pv g1f3 g8f6 d2d4 b8c6 b1c3
info depth 6 score cp 0 nodes 123969 nps 317056 time 391 pv g1f3 g8f6 d2d4 b8c6 d4d5 c6b4
info depth 7 score cp 35 nodes 1055309 nps 919258 time 1148 pv e2e4 g8f6 e4e5 f6d5 b1c3 d5c3 d2c3
info depth 8 score cp 5 nodes 4793595 nps 1132166 time 4234 pv b1c3 d7d5 e2e3 g8f6 f1b5 e6c8 b5c7 e6h3

info depth 1 score cp 50 nodes 20 nps 2500 time 8 pv g1f3
info depth 2 score cp 0 nodes 110 nps 11000 time 10 pv g1f3 g8f6
info depth 3 score cp 50 nodes 778 nps 59846 time 13 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3397 nps 135880 time 25 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 22171 nps 201554 time 110 pv g1f3 g8f6 d2d4 b8c6 b1c3
info depth 6 score cp 0 nodes 123969 nps 325377 time 381 pv g1f3 g8f6 d2d4 b8c6 d4d5 c6b4
info depth 7 score cp 35 nodes 1055309 nps 928969 time 1136 pv e2e4 g8f6 e4e5 f6d5 b1c3 d5c3 d2c3
info depth 8 score cp 5 nodes 4793595 nps 1180107 time 4062 pv b1c3 d7d5 e2e3 g8f6 f1b5 e6c8 b5c7 e6h3

info depth 1 score cp 50 nodes 20 nps 2500 time 8 pv g1f3
info depth 2 score cp 0 nodes 144 nps 13090 time 11 pv g1f3 g8f6
info depth 3 score cp 50 nodes 826 nps 59000 time 14 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3954 nps 136344 time 29 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 21646 nps 191557 time 113 pv g1f3 g8f6 b1c3 b8c6 e2e4
info depth 6 score cp 0 nodes 102719 nps 275386 time 373 pv g1f3 g8f6 b1c3 b8c6 e2e4 e7e5
info depth 7 score cp 35 nodes 564178 nps 769683 time 733 pv e2e4 d7d5 e4d5 d8d5 b1c3 d5e5 b1c3
info depth 8 score cp 5 nodes 3290512 nps 1150931 time 2859 pv b1c3 d7d5 e2e3 g8f6 f1b5 b8a6 c3a2 f6d7

info depth 1 score cp 50 nodes 20 nps 2857 time 7 pv g1f3
info depth 2 score cp 0 nodes 144 nps 14400 time 10 pv g1f3 g8f6
info depth 3 score cp 50 nodes 826 nps 63538 time 13 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3954 nps 146444 time 27 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 21646 nps 195009 time 111 pv g1f3 g8f6 b1c3 b8c6 e2e4
info depth 6 score cp 0 nodes 102719 nps 275386 time 373 pv g1f3 g8f6 b1c3 b8c6 e2e4 e7e5
info depth 7 score cp 35 nodes 564178 nps 774969 time 728 pv e2e4 d7d5 e4d5 d8d5 b1c3 d5e5 b1c3
info depth 8 score cp 5 nodes 3290512 nps 1190058 time 2765 pv b1c3 d7d5 e2e3 g8f6 f1b5 b8a6 c3a2 f6d7

info depth 1 score cp 50 nodes 20 nps 2857 time 7 pv g1f3
info depth 2 score cp 0 nodes 144 nps 16000 time 9 pv g1f3 g8f6
info depth 3 score cp 50 nodes 826 nps 68833 time 12 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3954 nps 158160 time 25 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 21646 nps 230276 time 94 pv g1f3 g8f6 b1c3 b8c6 e2e4
info depth 6 score cp 0 nodes 102719 nps 286125 time 359 pv g1f3 g8f6 b1c3 b8c6 e2e4 e7e5
info depth 7 score cp 35 nodes 564178 nps 816465 time 691 pv e2e4 d7d5 e4d5 d8d5 b1c3 d5e5 b1c3
info depth 8 score cp 5 nodes 3290512 nps 1245462 time 2642 pv b1c3 d7d5 e2e3 g8f6 f1b5 b8a6 c3a2 f6d7
info depth 9 score cp 25 nodes 12307306 nps 1230484 time 10002 pv e2e4 e7e5 g1f3 g8f6 f3e5 d7d6 e5c4 f6e4 b1c3

info depth 1 score cp 50 nodes 20 nps 2222 time 9 pv g1f3
info depth 2 score cp 0 nodes 144 nps 12000 time 12 pv g1f3 g8f6
info depth 3 score cp 50 nodes 826 nps 59000 time 14 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3954 nps 131800 time 30 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 21646 nps 251697 time 86 pv g1f3 g8f6 b1c3 b8c6 e2e4
info depth 6 score cp 0 nodes 102719 nps 376260 time 273 pv g1f3 g8f6 b1c3 b8c6 e2e4 e7e5
info depth 7 score cp 35 nodes 564178 nps 1072581 time 526 pv e2e4 d7d5 e4d5 d8d5 b1c3 d5e5 b1c3
info depth 8 score cp 5 nodes 3290512 nps 1486901 time 2213 pv b1c3 d7d5 e2e3 g8f6 f1b5 b8a6 c3a2 f6d7
info depth 9 score cp 25 nodes 13255337 nps 1554331 time 8528 pv e2e4 e7e5 g1f3 g8f6 f3e5 d7d6 e5c4 f6e4 b1c3

info depth 1 score cp 50 nodes 20 nps 2857 time 7 pv g1f3
info depth 2 score cp 0 nodes 144 nps 16000 time 9 pv g1f3 g8f6
info depth 3 score cp 50 nodes 826 nps 68833 time 12 pv g1f3 g8f6 b1c3
info depth 4 score cp 0 nodes 3954 nps 158160 time 25 pv g1f3 g8f6 b1c3 b8c6
info depth 5 score cp 40 nodes 21646 nps 237868 time 91 pv g1f3 g8f6 b1c3 b8c6 e2e4
info depth 6 score cp 0 nodes 102719 nps 292646 time 351 pv g1f3 g8f6 b1c3 b8c6 e2e4 e7e5
info depth 7 score cp 35 nodes 564178 nps 824821 time 684 pv e2e4 d7d5 e4d5 d8d5 b1c3 d5e5 b1c3
info depth 8 score cp 5 nodes 3290512 nps 1232401 time 2670 pv b1c3 d7d5 e2e3 g8f6 f1b5 b8a6 c3a2 f6d7
info depth 9 score cp 25 nodes 12076833 nps 1206717 time 10008 pv e2e4 e7e5 g1f3 g8f6 f3e5 d7d6 e5c4 f6e4 b1c3
bestmove e2e4

Looked at a total of 3195910687 Nodes in 35616 ms.
That is an avarge of 89732000 nps.

Looked at a total of 3195910687 Nodes in 33756 ms.
That is an avarge of 94676000 nps.

*/
UCI uci = new();
//Always listen UCI for command
//rom the GUI, and respond accordingly.
while (true)
{
    string message = Console.ReadLine();
    uci.ProcessCommand(message);
}
