Michael is a chess engine written in C#.  
It plays at an estimated strength of around 2400 ELO (about IM level), though it’s not close to the very top human players or the strongest engines.  
Michael uses a bitboard representation and modern search techniques such as iterative deepening, alpha-beta pruning, transposition tables, null-move pruning, quiescence search, and Piece square tables.  

This program implements the UCI protocol, so it’s meant to be used with a third-party GUI (like CuteChess or Arena), not directly run on its own. 
