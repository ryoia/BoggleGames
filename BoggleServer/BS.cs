//Written by Minwen Gao and Zachary Lobato - November, 2013 CS3500 - PS10

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BB;
using CustomNetworking;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
using BoggleSql;
using MySql.Data.MySqlClient;

namespace BoggleServer
{
    /// <summary>
    /// This is the Boggle Server class
    /// It will acts as a server, pairs up 2 clients in a game, and keep track of the status of the game
    /// </summary>
    public class BoggleServer
    {
        private HashSet<string> dictionary;

        static void Main(string[] args)
        {
            int timeDuration;
            string filePath;
            string boardCharacters = null;
            //create random boggle board if no 16-letter string has been sent through; 
            //otherwise, use the board being sent through the command line
            if (args.Length == 2)
            {
                if ((int.TryParse(args[0], out timeDuration)) && args[1] != null)
                {
                    filePath = args[1];
                    new BoggleServer(23, timeDuration, filePath, boardCharacters);
                }
            }
            else if (args.Length == 3)
            {
                if ((int.TryParse(args[0], out timeDuration)) && args[1] != null && args[2] != null && args[2].Length == 16)
                {
                    filePath = args[1];
                    boardCharacters = args[2];
                    new BoggleServer(23, timeDuration, filePath, boardCharacters);
                }
            }
            else if (args.Length == 0)
            {
                new BoggleServer(2000, 50, "..\\..\\..\\dictionary", null);

            }
            Console.ReadLine();
        }
        private TcpListener listener;
        private TcpListener webServerListener;
        private List<StringSocket> sockets;
        private List<Game> games;
        private Player waitingPlayer;
        int _timeDuration;
        string _boardCharacters;
        bool randomBoard;

        /// <summary>
        /// The Database Connection
        /// </summary>
        public const string connectionString = "server=atr.eng.utah.edu;database=minweng;uid=minweng;password=462415881";

        /// <summary>
        /// Boggle Server - The heart of the Boggle Program. This starts up a Game and Web Server
        /// -boardCharacters can be null (random board will be used)
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="timeDuration"></param>
        /// <param name="filePath"></param>
        /// <param name="boardCharacters"></param>
        public BoggleServer(int port, int timeDuration, string filePath, string boardCharacters)
        {
            _timeDuration = timeDuration;
            _boardCharacters = boardCharacters;

            if (boardCharacters == null)
                randomBoard = true;

            dictionary = new HashSet<string>();
            try
            {
                dictionary = HashSetFromFile(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception {0} was thrown", e.Message);
            }

            sockets = new List<StringSocket>();
            games = new List<Game>();
            waitingPlayer = null; //for only 1 player is connected

            // Listen for webpage vistors
            webServerListener = new TcpListener(IPAddress.Any, 2500);
            webServerListener.Start();
            webServerListener.BeginAcceptSocket(WebServerConnectionRequested, null);

            // Listen for connections to game
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.BeginAcceptSocket(ConnectionRequested, null);
            
        }

        // WEB SERVER

        /// <summary>
        /// Callback for Web Server Listener
        /// -Creates a socket between client and server.
        /// -Begins receiving data from the client (URL information)
        /// -Recalls WebServerListener to listen for more page views.
        /// </summary>
        private void WebServerConnectionRequested(IAsyncResult result)
        {
            StringSocket ss = new StringSocket(webServerListener.EndAcceptSocket(result), new UTF8Encoding());
            ss.BeginReceive(WebRequestServerStart, ss);
            webServerListener.BeginAcceptSocket(WebServerConnectionRequested, null);
        }

        /// <summary>
        /// Callback for WebServer String Socket
        /// -Parses URL sent back to create new web page for user.
        /// </summary>
        private void WebRequestServerStart(string url, Exception e, object ss)
        {
            StringSocket socket = (StringSocket)ss;

            if (url != null)
            {
                // Determine WebPage to be displayed
                if (url.Substring(0, 4).Equals("GET "))
                {
                    string newUrl = url.Substring(3);
                    if (newUrl.Equals(" /players HTTP/1.1\r"))
                    {
                        PlayerRecordsWebPage(socket);
                    }
                    else if (newUrl.Contains("/games?player="))
                    {
                        int index = newUrl.IndexOf("=");
                        int index2 = newUrl.IndexOf(" HTTP");
                        string playerName = newUrl.Substring(index + 1, index2 - index - 1);
                        PlayerGameHistoryWebPage(socket, playerName);
                    }
                    else if (newUrl.Contains("/game?id="))
                    {
                        int index = newUrl.IndexOf("=");
                        int index2 = newUrl.IndexOf(" HTTP");

                        int gameID;

                        // Ensure valid integer is used
                        bool validID = Int32.TryParse(newUrl.Substring(index + 1, index2 - index - 1), out gameID);

                        if (validID)
                            GameSummaryWebPage(socket, gameID);
                        else
                            ErrorWebPage(socket);
                    }
                    // If none of the above WebPages are correct, return Error WebPage.
                    else
                    {
                        ErrorWebPage(socket);
                    }
                }
            }
            //socket.Close();
        }

        /// <summary>
        /// Closes Sockets after WebPage has fully loaded.
        /// </summary>
        private void CloseSocketCallback(StringSocket ss)
        {
            if (ss != null)
            {
                ss.Close();
            }
        }

        // HELPER METHODS

        /// <summary>
        /// WebPage containing record of each player.
        /// </summary>
        private void PlayerRecordsWebPage(StringSocket ss)
        {
            using (MySqlConnection connect = new MySqlConnection(connectionString))
            {
                // Open MySqlConnection to Database
                connect.Open();

                MySqlCommand command = connect.CreateCommand();
                MySqlDataReader reader;

                // Begin to send HTML Web Page
                ss.BeginSend("HTTP/1.1 200 OK\r\n", (ee, pp) => { }, null);
                ss.BeginSend("Connection: close\r\n", (ee, pp) => { }, null);
                ss.BeginSend("Content-Type: text/html; charset=UTF-8\r\n", (ee, pp) => { }, null);
                ss.BeginSend("\r\n", (ee, pp) => { }, null);

                // Store HTML Page
                List<String> webPage = new List<String>();
                webPage.Add("<html>");
                webPage.Add("<title>Boggle Records</title>");
                webPage.Add("<body>");
                webPage.Add("<h1>Boggle Players Records</h1>");
                webPage.Add("<table border=\"1\">");
                webPage.Add("<tr><td>Player</td><td>Games Won</td><td>Games Lost</td><td>Games Tied</td></tr>");


                // Sorted Dictionary to Store Wicked-Awesome Query
                SortedDictionary<String, Int32[]> playerWinsLossesTies = new SortedDictionary<String, Int32[]>();

                // Query for Tied (Bool), PlayerAName, PlayerBName, WinnerName, LoserName
                command.CommandText = @"
                                    SELECT Tied,
                                        PlayerAName.PlayerName AS 'Tie A Name',
                                        PlayerBName.PlayerName AS 'Tie B Name' ,
                                        WinName.PlayerName AS 'Winner', 
                                        LoseName.PlayerName AS 'Loser'
                                    FROM GameInfo AS Game 
                                         LEFT JOIN Players AS PlayerAName 
                                                   ON Game.PlayerAID=PlayerAName.PlayerID
                                         LEFT JOIN Players AS PlayerBName 
                                                   ON Game.PlayerBID=PlayerBName.PlayerID
                                         LEFT JOIN Players AS WinName 
                                                   ON Game.WinnerName=WinName.PlayerID
                                         LEFT JOIN Players AS LoseName 
                                                   ON Game.LoserName=LoseName.PlayerID";
                command.Parameters.Clear();

                using (reader = command.ExecuteReader())
                {
                    // For every result from the Query
                    while (reader.Read())
                    {
                        // If tied is false, then we know there must be a winner
                        if (reader.GetInt32(0) == 0)
                        {
                            String keyNameWinner;
                            String keyNameLoser;
                            try
                            {
                                keyNameWinner = reader.GetString(3);
                                keyNameLoser = reader.GetString(4);
                            }
                            catch
                            {
                                continue;
                            }
                            
                            // Add one point to Winner Score
                            if (playerWinsLossesTies.ContainsKey(keyNameWinner))
                            {
                                playerWinsLossesTies[keyNameWinner][0] += 1;
                            }
                            else
                            {
                                playerWinsLossesTies.Add(keyNameWinner, new int[]{1,0,0});
                            }

                            // Add one point to Loser Score
                            if (playerWinsLossesTies.ContainsKey(keyNameLoser))
                            {
                                playerWinsLossesTies[keyNameLoser][1] += 1;
                            }
                            else
                            {
                                playerWinsLossesTies.Add(keyNameLoser, new int[] { 0, 1, 0 });
                            }
                        }
                        // Both Player A and Player B must be Tied
                        else if (reader.GetInt32(0) == 1)
                        {
                            String keyTiedPlayerA;
                            String keyTiedPlayerB;

                            try
                            {
                                keyTiedPlayerA = reader.GetString(1);
                                keyTiedPlayerB = reader.GetString(2);
                            }
                            catch
                            {
                                continue;
                            }

                            // Add point to both players tied scores
                            if (playerWinsLossesTies.ContainsKey(keyTiedPlayerA))
                            {
                                playerWinsLossesTies[keyTiedPlayerA][2] += 1;
                            }
                            else
                            {
                                playerWinsLossesTies.Add(keyTiedPlayerA, new int[] { 0, 0, 1 });
                            }

                            if (playerWinsLossesTies.ContainsKey(keyTiedPlayerB))
                            {
                                playerWinsLossesTies[keyTiedPlayerB][2] += 1;
                            }
                            else
                            {
                                playerWinsLossesTies.Add(keyTiedPlayerB, new int[] { 0, 0, 1 });
                            }
                        }

                    }
                }

                foreach (KeyValuePair<String, Int32[]> row in playerWinsLossesTies)
                {
                    webPage.Add("<tr><td><a href=\"/games?player=" + row.Key + "\">" + row.Key + "</a></td><td>" + row.Value[0] + "</td><td>" + row.Value[1] + "</td><td>" + row.Value[2] + "</td></tr>");
                }

                webPage.Add("</table>");


                webPage.Add("</body>");
                webPage.Add("</html>\n");

                StringBuilder webPageAsString = new StringBuilder();
                foreach (String line in webPage)
                {
                    webPageAsString.Append(line);
                }

                // Send webpage to user.
                ss.BeginSend(webPageAsString.ToString(), (ee, pp) => { CloseSocketCallback(ss); }, null);
            }
        }

        /// <summary>
        /// WebPage containing an individual player's Game History
        /// </summary>
        private void PlayerGameHistoryWebPage(StringSocket ss, string playerName)
        {
            using (MySqlConnection connect = new MySqlConnection(connectionString))
            {
                // Open MySqlConnection to Database
                connect.Open();

                using (MySqlCommand command = connect.CreateCommand())
                {
                    // Query for Player ID and Count
                    command.CommandText = "select PlayerID, count(*) from Players where PlayerName = @playerName;";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@playerName", playerName);
                    command.Prepare();

                    int numRowsWithPlayerID = 0;
                    int playerID = 0;

                    MySqlDataReader reader;

                    // Read query results and store Player ID
                    using (reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            numRowsWithPlayerID = reader.GetInt32(1);

                            try
                            {
                                playerID = reader.GetInt32(0);
                            }
                            catch
                            {
                            }
                        }
                    }

                    // If Query for PlayerID yields no rows, send to ErrorWebPage
                    if (numRowsWithPlayerID == 0)
                    {
                        ErrorWebPage(ss);
                    }
                    // Else build WebPage
                    else
                    {
                        // Begin to send HTML Web Page
                        ss.BeginSend("HTTP/1.1 200 OK\r\n", (ee, pp) => { }, null);
                        ss.BeginSend("Connection: close\r\n", (ee, pp) => { }, null);
                        ss.BeginSend("Content-Type: text/html; charset=UTF-8\r\n", (ee, pp) => { }, null);
                        ss.BeginSend("\r\n", (ee, pp) => { }, null);

                        // WebPage as List
                        List<String> webPage = new List<String>();

                        webPage.Add("<html>");
                        webPage.Add("<title>"+ playerName + ((playerName[playerName.Length-1]==('s'))?"\'":"\'s") + " Game History</title>");
                        webPage.Add("<body>");
                        webPage.Add("<h1>"+ playerName + ((playerName[playerName.Length-1]==('s'))?"\'":"\'s") + " Game History</h1>");
                        webPage.Add("<table border=\"1\">");

                        // Table column headers
                        webPage.Add("<tr><td>Game ID</td><td>Date</td><td>Time</td><td>Opponent Name</td><td>Player Score</td><td>Opponent Score</td></tr>");

                        string opponentName = "";
                        bool isPlayerA = false;
                        //get the game IDs associate to the playerID
                        List<int> gameIDs = new List<int>();
                        command.CommandText = "select GameID from GameInfo where (PlayerAID = @playerID or PlayerBID = @playerID);";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@playerID", playerID);
                        command.Prepare();
                        using (reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                gameIDs.Add(reader.GetInt32(0));
                            }
                        }

                        // Get EndDate Time, PlayerA's Score, and Player B's Score For Each game
                        
                        foreach (int gameID in gameIDs)
                        {
                            // Query for EndDate Time, PlayerA's Score, and Player B's Score
                            opponentName = CheckPlayerAOrB(command, ref reader, gameID, playerID, ref isPlayerA);
                            command.CommandText = "select EndDateTime, ScoreA, ScoreB from GameInfo where GameID = @gameID;";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@gameID", gameID);
                            command.Prepare();

                            string endDateTime = "";
                            int playerAscore = 0;
                            int playerBscore = 0;
                            using (reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    endDateTime = reader.GetString(0);
                                    playerAscore = reader.GetInt32(1);
                                    playerBscore = reader.GetInt32(2);
                                }
                            }

                            // Regex for Date and Time
                            Regex dateRegex = new Regex(@"\d+/\d+/\d+");
                            Regex timeRegex = new Regex(@"\d+:\d+:\d+ [AP]M");

                            

                            // Depending on if player is A or B in game, add row to table with correct formatting.
                            if (isPlayerA)
                                webPage.Add("<tr><td><a href=\"/game?id=" + gameID + "\">" + gameID + "</a></td><td>" + dateRegex.Match(endDateTime).ToString() + "</td><td>" + timeRegex.Match(endDateTime).ToString() + "</td><td><a href=\"/games?player=" + opponentName + "\">" + opponentName + "</a></td><td>" + playerAscore + "</td><td>" + playerBscore + "</td></tr>");
                            else
                                webPage.Add("<tr><td><a href=\"/game?id=" + gameID + "\">" + gameID + "</a></td><td>" + dateRegex.Match(endDateTime).ToString() + "</td><td>" + timeRegex.Match(endDateTime).ToString() + "</td><td><a href=\"/games?player=" + opponentName + "\">" + opponentName + "</a></td><td>" + playerBscore + "</td><td>" + playerAscore + "</td></tr>");
                        }

                        webPage.Add("</table>");
                        webPage.Add("<a href=\"/players\">Boggle Records</a>");
                        webPage.Add("</body>");
                        webPage.Add("</html>\n");

                        StringBuilder webPageAsString = new StringBuilder();

                        foreach (String line in webPage)
                        {
                            webPageAsString.Append(line);
                        }

                        // Send WebPage to Client
                        ss.BeginSend(webPageAsString.ToString(), (ee, pp) => { CloseSocketCallback(ss); }, null);
                    }
                }
            }

        }

        /// <summary>
        /// WebPage containing the summary of an individual Game
        /// </summary>
        private void GameSummaryWebPage(StringSocket ss, int gameID)
        {
            using (MySqlConnection connect = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = connect.CreateCommand())
                {
                    // Open MySqlConnection to Database
                    connect.Open();

                    // Query for GameID and Count
                    command.CommandText = "select count(*) from GameInfo where GameID = @gameID";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@gameID", gameID);
                    command.Prepare();

                    MySqlDataReader reader;

                    int numRowsWithGameID = 0;
                    using (reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            numRowsWithGameID = reader.GetInt32(0);
                        }
                    }

                    // If Query for GameID yields no rows, send to Error Web Page
                    if (numRowsWithGameID == 0)
                    {
                        ErrorWebPage(ss);
                    }
                    else
                    {
                        // Begin to send HTML Web Page
                        ss.BeginSend("HTTP/1.1 200 OK\r\n", (ee, pp) => { }, null);
                        ss.BeginSend("Connection: close\r\n", (ee, pp) => { }, null);
                        ss.BeginSend("Content-Type: text/html; charset=UTF-8\r\n", (ee, pp) => { }, null);
                        ss.BeginSend("\r\n", (ee, pp) => { }, null);

                        // Store Web Page
                        List<String> webPage = new List<String>();

                        webPage.Add("<html>");
                        webPage.Add("<title>Boggle Game "+ gameID +" - Summary</title>");
                        webPage.Add("<body>");
                        webPage.Add("<h1>Game " + gameID + " Summary</h1>");

                        // Query for PlayerAID, Score A, SummaryA, PlayerBID, Score B, SummaryB, End Date Time, Time Limit, and Board
                        command.CommandText = "select PlayerAID, ScoreA, SummaryA, PlayerBID, ScoreB, SummaryB, EndDateTime, TimeLimit, Board from GameInfo where GameID = @gameID;";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@gameID", gameID);
                        command.Prepare();

                        int playerAID = 0;
                        int playerBID = 0;
                        int playerAScore = 0;
                        int playerBScore = 0;
                        string playerASummary = "";
                        string playerBSummary = "";
                        string endDateTime = "";
                        int timeLimit = 0;
                        string board = "";

                        using (reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                playerAID = reader.GetInt32(0);
                                playerAScore = reader.GetInt32(1);
                                playerASummary = reader.GetString(2);
                                playerBID = reader.GetInt32(3);
                                playerBScore = reader.GetInt32(4);
                                playerBSummary = reader.GetString(5);
                                endDateTime = reader.GetString(6);
                                timeLimit = reader.GetInt32(7);
                                board = reader.GetString(8);
                            }
                        }

                        // Query for PlayerA and PlayerB Names
                        string playerAName = "";
                        string playerBName = "";

                        playerAName = GetNameFromID(ref reader, playerAID, command);
                        playerBName = GetNameFromID(ref reader, playerBID, command);

                        // Create Table for PlayerA and PlayerB Stats
                        webPage.Add("<h3>Time Limit: " + timeLimit + "</br>Date & Time: " + endDateTime + "</br></h3>");
                        webPage.Add("<table border = \"1\">");
                        webPage.Add("<tr><td>Player A's Name</td><td><a href=\"/games?player=" + playerAName + "\">" + playerAName + "</a></td></tr>");
                        webPage.Add("<tr><td>Player A's Score</td><td>" + playerAScore + "</td></tr>");
                        webPage.Add("<tr><td>Player A's Summary</td><td>" + playerASummary + "</td></tr>");
                        webPage.Add("</table>");

                        webPage.Add("<table border = \"1\">");
                        webPage.Add("<tr><td>Player B's Name</td><td><a href=\"/games?player=" + playerBName + "\">" + playerBName + "</a></td></tr>");
                        webPage.Add("<tr><td>Player B's Score</td><td>" + playerBScore + "</td></tr>");
                        webPage.Add("<tr><td>Player B's Summary</td><td>" + playerBSummary + "</td></tr>");
                        webPage.Add("</table>");

                        webPage.Add("<p>");

                        // Create Table of Game Board
                        webPage.Add("<table border = \"1\" bgcolor=\"#FF8000\" cellspacing=\"5\" cellpadding=\"4\">");
                        webPage.Add("</tr> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[0].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[1].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[2].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[3].ToString() + ".png\"></td> </tr>");
                        webPage.Add("</tr> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[4].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[5].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[6].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[7].ToString() + ".png\"></td> </tr>");
                        webPage.Add("</tr> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[8].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[9].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[10].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[11].ToString() + ".png\"></td> </tr>");
                        webPage.Add("</tr> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[12].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[13].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[14].ToString() + ".png\"></td> <td bgcolor=\"maroon\"><img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/" + board[15].ToString() + ".png\"></td> </tr>");
                        webPage.Add("</table>");

                        webPage.Add("<a href=\"/players\">Boggle Records</a>");

                        webPage.Add("</body>");
                        webPage.Add("</html>\n");

                        StringBuilder webPageAsString = new StringBuilder();

                        foreach (String line in webPage)
                        {
                            webPageAsString.Append(line);
                        }

                        // Send rest of Web Page
                        ss.BeginSend(webPageAsString.ToString(), (ee, pp) => { CloseSocketCallback(ss); }, null);
                    }
                }
            }
            
        }

        /// <summary>
        /// WebPage containing Error Message and Helpful Information
        /// </summary>
        private void ErrorWebPage(StringSocket ss)
        {
            using (MySqlConnection connect = new MySqlConnection(connectionString))
            {
                // Begin to send HTML Web Page
                ss.BeginSend("HTTP/1.1 200 OK\r\n", (ee, pp) => { }, null);
                ss.BeginSend("Connection: close\r\n", (ee, pp) => { }, null);
                ss.BeginSend("Content-Type: text/html; charset=UTF-8\r\n", (ee, pp) => { }, null);
                ss.BeginSend("\r\n", (ee, pp) => { }, null);

                // Store Web Page as List
                List<String> webPage = new List<String>();

                webPage.Add("<html>");
                webPage.Add("<title>Exist, this does not!</title>");
                webPage.Add("<body>");
                webPage.Add("<center>");
                webPage.Add("<h1>\"This isn't the page you're looking for.\"</h1>");
                webPage.Add("<img src = \"https://dl.dropboxusercontent.com/u/87615358/Images/ObiWan.jpg\">");
                webPage.Add("</center>");
                webPage.Add("<p>");
                webPage.Add("<h3>This URL contains nothing.</h3></br>Please change the URL to match the following format: </br>");
                webPage.Add("<ul style=\"list-style-type:circle\">");
                webPage.Add("<li><b>/players</b></li><ul><li><i>a web page of the Player Records will display</i></li></ul></br>");
                webPage.Add("<li><b>/games?player=PLAYERNAME</b> (Replacing PLAYERNAME with a legit player name)</li><ul><li><i>a web page containing their Games History will display</i></li></ul></br>");
                webPage.Add("<li><b>/game?id=GAMEID</b> (Replacing GAMEID with a legit game ID)</li><ul><li><i>a web page containing a Game Summary will display</i></li></ul>");
                webPage.Add("</ul>");

                webPage.Add("<a href=\"/players\">Boggle Records</a>");

                webPage.Add("</body>");
                webPage.Add("</html>\n");

                StringBuilder webPageAsString = new StringBuilder();

                foreach (String line in webPage)
                {
                    webPageAsString.Append(line);
                }

                // Send rest of Web Page
                ss.BeginSend(webPageAsString.ToString(), (ee, pp) => { CloseSocketCallback(ss); }, null);
            }
        }


        /// <summary>
        /// Private Helper Method
        /// -Returns Name from DB linked to ID
        /// </summary>
        private static string GetNameFromID(ref MySqlDataReader reader, int playerID, MySqlCommand command)
        {
            // Query for Name from Player ID
            string playerName = "";
            command.CommandText = "select PlayerName from Players where PlayerID = @playerID;";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@playerID", playerID);
            command.Prepare();
            using (reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    playerName = reader.GetString(0);
                }
            }

            // Return result
            return playerName;
        }


        /// <summary>
        /// Private Helper Method
        /// -Returns opponent player name
        /// -Sets reference bool to determine if player "isPlayerA" or not.
        /// </summary>
        private string CheckPlayerAOrB(MySqlCommand command, ref MySqlDataReader reader, int gameID, int playerID, ref bool isPlayerA)
        {
            // Query for PlayerAID and PlayerBID and store IDs
            int playerAId = 0;
            int playerBId = 0;

            command.CommandText = "select PlayerAID, PlayerBID from GameInfo where GameID = @gameID";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@gameID", gameID);
            command.Prepare();
            using (reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    playerAId = reader.GetInt32(0);
                    playerBId = reader.GetInt32(1);
                }
            }


            // Determine if playerID is for Player A or Player B and store Name
            string opponentName = "";

            if (playerID == playerAId)
            {
                isPlayerA = true;

                // Query for Opponent's Name (As Player B)
                command.CommandText = "select PlayerName from Players where PlayerID = @playerBID;";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@playerBID", playerBId);
                command.Prepare();
                using (reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        opponentName = reader.GetString(0);
                    }
                }
            }
            else
            {
                isPlayerA = false;

                // Query for Opponent's Name (As Player A)
                command.CommandText = "select PlayerName from Players where PlayerID = @playerAID;";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@playerAID", playerAId);
                command.Prepare();
                using (reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        opponentName = reader.GetString(0);
                    }
                }
            }

            // Return Opponent's Name
            return opponentName;
        }

        // GAME SERVER

        /// <summary>
        /// To establish connection through StringSocket (begin listening)
        /// </summary>
        /// <param name="result"></param>
        public void ConnectionRequested(IAsyncResult result)
        {
            // We obtain the socket corresonding to the connection request.  Notice that we
            // are passing back the IAsyncResult object.
            StringSocket ss = new StringSocket(listener.EndAcceptSocket(result), new UTF8Encoding());

            ss.BeginReceive(RequestGameStart, ss);

            // We ask the server to listen for another connection request.  As before, this
            // will happen on another thread.
            listener.BeginAcceptSocket(ConnectionRequested, null);
        }

        /// <summary>
        /// Callback Method
        /// -Takes a StringSocket as the payload to make sure each game has unique StringSocket
        /// -When two clients are paired, creates a game
        /// </summary>
        private void RequestGameStart(string playCommand, Exception e, object o)
        {
            StringSocket ss = (StringSocket)o;

            if (playCommand == null)
            {
                ss.Close();
                return;
            }

            playCommand = playCommand.ToUpper();
            lock (sockets)
            {

                if (playCommand.Substring(0, 5).Equals("PLAY "))
                {
                    // Check if player is waiting
                    if (waitingPlayer == null)
                    {
                        waitingPlayer = new Player(playCommand.Substring(5), ss);
                        waitingPlayer.SS.BeginReceive(invalidword, waitingPlayer);
                        Console.WriteLine("Player waiting for a game.");
                    }
                    else //Pair 2 players into a game
                    {
                        Console.WriteLine("Opponent connected. A new game has started.");
                        Player challengingPlayer = new Player(playCommand.Substring(5), ss);
                        challengingPlayer.SS.BeginReceive(invalidword, challengingPlayer);
                        if (randomBoard)
                        {
                            games.Add(new Game(waitingPlayer, challengingPlayer, _timeDuration, dictionary));
                        }
                        else
                        {
                            games.Add(new Game(waitingPlayer, challengingPlayer, _timeDuration, _boardCharacters, dictionary));
                        }
                        waitingPlayer = null;
                    }
                }
                sockets.Add(ss);
            }
        }

        /// <summary>
        /// Callback Method 
        /// -Used to ignore invalid words sent into game, prior to game starting.
        /// </summary>
        private void invalidword(string s, Exception e, object payload)
        {
            Player player = (Player)payload;
            if (!player.PlayingGame)
            {
                player.SS.BeginSend("IGNORING " + s + "\n", (ee, pp) => { }, null);
                player.SS.BeginReceive(invalidword, payload);
            }
            else
            {
                Player otherPlayer;

                if (player == player.MyGame.PlayerA)
                {
                    otherPlayer = player.MyGame.PlayerB;
                }
                else
                    otherPlayer = player.MyGame.PlayerA;

                player.MyGame.IncomingWordsCallBackPlayer(s, e, payload, player, otherPlayer);
            }
        }

        /// <summary>
        /// Helper Method
        /// -Reads a Dictionary of words
        /// -Returns dictionary as String HashSet
        /// </summary>
        private static HashSet<String> HashSetFromFile(String filePath)
        {
            HashSet<string> hashDict = new HashSet<string>();

            // Import Dictionary As All Uppercase
            string dictionary = System.IO.File.ReadAllText(filePath).ToUpper();

            // Split dictionary into array of Words (ignoring whitespace)
            String[] words = Regex.Split(dictionary, @"\W+");

            // Add each word into hashDictionary
            foreach (String word in words)
            {
                hashDict.Add(word);
            }

            return hashDict;
        }
    }

    /// <summary>
    /// Player Class
    /// -Contains important player information 
    ///     +Player's Legal/Illegal Words (As List)
    ///     +Player's name
    ///     +Player's score
    ///     +Player's StringSocket
    /// </summary>
    class Player
    {
        int _score;
        string _playerName;
        SortedSet<string> _guessedWords;
        SortedSet<string> _illegalWords;
        StringSocket _ss;
        Game _myGame;
        bool _playingGame;

        public bool PlayingGame
        {
            get { return _playingGame; }
            set { _playingGame = value; }
        }

        public Game MyGame
        {
            get { return _myGame; }
            set { _myGame = value; }
        }

        public int Score
        {
            get { return _score; }
            set { _score = value; }
        }

        public SortedSet<string> Words
        {
            get { return _guessedWords; }
        }

        public SortedSet<string> IllegalWords
        {
            get { return _illegalWords; }
        }

        public StringSocket SS
        {
            get { return _ss; }
        }

        public string PlayerName
        {
            get { return _playerName; }
        }

        public Player(string name, StringSocket ss)
        {
            _playerName = name;
            _score = 0;
            _guessedWords = new SortedSet<string>();
            _illegalWords = new SortedSet<string>();
            _ss = ss;

            _playingGame = false;
            _myGame = null;
        }
    }

    /// <summary>
    /// A class to update time every second to each player in the game
    /// Also controls the end of game message (including each player's final result)
    /// </summary>
    class TimeAndScoreUpdater
    {
        private int _currentGameClock;

        public TimeAndScoreUpdater(int gameDuration)
        {
            _currentGameClock = gameDuration;
        }

        public void UpdateTime(object payload)
        {

            Game currentGame = (Game)payload;
            Game.SendMessage(currentGame.PlayerA, "TIME " + _currentGameClock + "\n");
            Game.SendMessage(currentGame.PlayerB, "TIME " + _currentGameClock + "\n");
            //Give players their final results (score, lists of various words)
            if (_currentGameClock == 0)
            {
                /*
                 * Suppose that during the game the client played 
                 * a legal words that weren't played by the opponent, the opponent played 
                 * b legal words that weren't played by the client, both players played 
                 * c legal words in common, the client played 
                 * d illegal words, and the opponent played 
                 * e illegal words. 
                 * The game summary command should be 
                 * "STOP a #1 b #2 c #3 d #4 e #5", where 
                 * a, b, c, d, and e are the counts described above and 
                 * #1, #2, #3, #4, and #5 are the corresponding space-separated lists of words.
                 */

                string aALegalWords, bBLegalWords, cCommonWords, dAIllegalWords, eBIllegalWords;

                aALegalWords = SpaceSeparatedStringOfWords(currentGame.PlayerA.Words);
                bBLegalWords = SpaceSeparatedStringOfWords(currentGame.PlayerB.Words);
                cCommonWords = SpaceSeparatedStringOfWords(currentGame.CommonWords);
                dAIllegalWords = SpaceSeparatedStringOfWords(currentGame.PlayerA.IllegalWords);
                eBIllegalWords = SpaceSeparatedStringOfWords(currentGame.PlayerB.IllegalWords);

                string playerASummary = "STOP " + currentGame.PlayerA.Words.Count + " " + aALegalWords + " "
                                                         + currentGame.PlayerB.Words.Count + " " + bBLegalWords + " "
                                                         + currentGame.CommonWords.Count + " " + cCommonWords + " "
                                                         + currentGame.PlayerA.IllegalWords.Count + " " + dAIllegalWords + " "
                                                         + currentGame.PlayerB.IllegalWords.Count + " " + eBIllegalWords + " " + "\n";

                string playerBSummary = "STOP " + currentGame.PlayerB.Words.Count + " " + bBLegalWords + " "
                                                         + currentGame.PlayerA.Words.Count + " " + aALegalWords + " "
                                                         + currentGame.CommonWords.Count + " " + cCommonWords + " "
                                                         + currentGame.PlayerB.IllegalWords.Count + " " + eBIllegalWords + " "
                                                         + currentGame.PlayerA.IllegalWords.Count + " " + dAIllegalWords + " " + "\n";
                Game.SendMessage(currentGame.PlayerA, playerASummary);

                Game.SendMessage(currentGame.PlayerB, playerBSummary);

                Console.Write("\r\n" + currentGame.PlayerA.PlayerName + " - ");
                Console.WriteLine("STOP " + currentGame.PlayerA.Words.Count + " " + aALegalWords + " "
                                                         + currentGame.PlayerB.Words.Count + " " + bBLegalWords + " "
                                                         + currentGame.CommonWords.Count + " " + cCommonWords + " "
                                                         + currentGame.PlayerA.IllegalWords.Count + " " + dAIllegalWords + " "
                                                         + currentGame.PlayerB.IllegalWords.Count + " " + eBIllegalWords + " " + "\n");
                Console.Write(currentGame.PlayerB.PlayerName + " - ");
                Console.WriteLine("STOP " + currentGame.PlayerB.Words.Count + " " + bBLegalWords + " "
                                                         + currentGame.PlayerA.Words.Count + " " + aALegalWords + " "
                                                         + currentGame.CommonWords.Count + " " + cCommonWords + " "
                                                         + currentGame.PlayerB.IllegalWords.Count + " " + eBIllegalWords + " "
                                                         + currentGame.PlayerA.IllegalWords.Count + " " + dAIllegalWords + " " + "\n");
                //close down both sockets afterward
                currentGame.PlayerA.SS.Close();
                currentGame.PlayerB.SS.Close();

                currentGame.GameClock.Dispose();
                BoggleSql.Program.WritingToGameInfoDB(currentGame.PlayerA.PlayerName, currentGame.PlayerB.PlayerName, currentGame.PlayerA.Score, currentGame.PlayerB.Score, playerASummary,
                    playerBSummary, DateTime.Now.ToString(), currentGame.GameDuration, currentGame.BB.ToString());
            }
            _currentGameClock--;
        }

        /// <summary>
        /// Helper Method
        /// -Parses a set of words and returns them as a String separated by spaces.
        /// 
        /// </summary>
        public static string SpaceSeparatedStringOfWords(SortedSet<string> wordSet)
        {
            StringBuilder wordsAsLongString = new StringBuilder("");

            foreach (String word in wordSet)
            {
                wordsAsLongString.Append(word + " ");
            }

            return wordsAsLongString.ToString().Trim();
        }
    }

    /// <summary>
    /// Game Class
    ///     -Contains important Game information
    ///         +Game Time
    ///         +Game Players
    ///         +Game Dictionary
    ///         +Game Board
    ///         +Common Words Played
    ///         +Game TimeDuration
    /// </summary>
    class Game
    {
        int _gameDuration;
        Player _playerA;
        Player _playerB;
        BoggleBoard _bb;
        HashSet<string> _dictionary;
        SortedSet<string> _commonWords;
        Timer _gameClock;
        Object _lockPoints = new Object();

        public Player PlayerA
        {
            get { return _playerA; }
        }

        public int GameDuration
        {
            get { return _gameDuration; }
        }

        public Player PlayerB
        {
            get { return _playerB; }
        }

        public Timer GameClock
        {
            get { return _gameClock; }
        }

        public SortedSet<string> CommonWords
        {
            get { return _commonWords; }
        }

        public BoggleBoard BB
        {
            get { return _bb; }
        }

        public Game(Player playerA, Player playerB, int duration, HashSet<string> dictionary)
            : this(playerA, playerB, duration, null, dictionary)
        {
            // Calls overloaded constructor with null as parameter
        }

        /// <summary>
        /// Constructor. Also sends players the information regarding the game ("START", game duration, and opponent's name)
        /// </summary>
        /// <param name="playerA">Player</param>
        /// <param name="playerB">Player</param>
        /// <param name="duration">Game Length</param>
        /// <param name="lettersOnBoard">16 Letters used for Board</param>
        /// <param name="dictionary">Dictionary used for Game</param>
        public Game(Player playerA, Player playerB, int duration, string lettersOnBoard, HashSet<string> dictionary)
        {
            if (lettersOnBoard == null)
                _bb = new BoggleBoard();
            else
                _bb = new BoggleBoard(lettersOnBoard);

            _playerA = playerA;
            _playerB = playerB;

            _gameDuration = duration;
            _dictionary = dictionary;

            _commonWords = new SortedSet<string>();

            SendMessage(_playerA, "START " + _bb.ToString() + " " + duration + " " + _playerB.PlayerName + "\n");
            SendMessage(_playerB, "START " + _bb.ToString() + " " + duration + " " + _playerA.PlayerName + "\n");

            TimeAndScoreUpdater timeAndScoreUpdater = new TimeAndScoreUpdater(_gameDuration);

            _playerA.MyGame = this;
            _playerB.MyGame = this;

            _playerA.PlayingGame = true;
            _playerB.PlayingGame = true;

            _gameClock = new Timer(timeAndScoreUpdater.UpdateTime, this, 0, 1000);
            _playerA.SS.BeginReceive((string s, Exception e, object p) => IncomingWordsCallBackPlayer(s, e, p, _playerA, _playerB), null);
            _playerB.SS.BeginReceive((string s, Exception e, object p) => IncomingWordsCallBackPlayer(s, e, p, _playerB, _playerA), null);
        }

        /// <summary>
        /// Helper Method
        /// -Convience method to send messages.
        /// </summary>
        internal static void SendMessage(Player player, string sendMessage)
        {
            player.SS.BeginSend(sendMessage, (ee, pp) => { }, null);
        }

        /// <summary>
        /// Callback Method
        /// -Notifies player if their opponent closes the socket before the Game ends.
        /// </summary>
        public void IncomingWordsCallBackPlayer(string msg, Exception ex, object payload, Player player1, Player player2)
        {
            if (ex == null)
            {
                if (msg != null)
                {
                    msg = msg.ToUpper();
                    if (msg.Length >= 5)
                    {
                        string trueWord = msg.Substring(5);
                        string testWord = msg.Substring(0, 5);
                        if (testWord == "WORD ")
                        {
                            if (trueWord.Length >= 3)
                            {
                                lock (player1)
                                {
                                    lock (player2)
                                    {
                                        CalculatePointsForGuessedWord(player1, player2, trueWord.ToUpper().Trim());
                                    }

                                }
                            }
                            player1.SS.BeginReceive((string s, Exception e, object p) => IncomingWordsCallBackPlayer(s, e, p, player1, player2), null);
                        }
                        else
                            player1.SS.BeginReceive((string s, Exception e, object p) => IncomingWordsCallBackPlayer(s, e, p, player1, player2), null);
                    }
                    else
                    {
                        player1.SS.BeginSend("IGNORING " + msg + "\n", (ee, pp) => { }, null);
                        player1.SS.BeginReceive((string s, Exception e, object p) => IncomingWordsCallBackPlayer(s, e, p, player1, player2), null);
                    }
                }
                else
                {
                    if (payload == null)
                    {
                        SendMessage(player2, "TERMINATED\n");
                        player2.SS.Close();
                    }
                }

            }
            else
            {
                SendMessage(player2, "TERMINATED\n");
                player2.SS.Close();
            }

        }

        /// <summary>
        /// Helper Method
        /// -Calculates the points earned or lost on each valid guess of a word.
        /// </summary>
        private void CalculatePointsForGuessedWord(Player player, Player otherPlayer, string guess)
        {
            Console.WriteLine(player.PlayerName + " - " + guess);
            // If word has already been played
            if (!player.Words.Contains(guess) && !player.IllegalWords.Contains(guess) && !_commonWords.Contains(guess))
            {
                // Check against dictionary
                if (_dictionary.Contains(guess))
                {
                    // If in Dict, check against board
                    if (!_bb.CanBeFormed(guess))
                    {
                        // Word is illegal
                        TreatIllegalWords(player, guess);
                        ShowUpdatedScoreToClients(player, otherPlayer);
                    }
                    else
                    {
                        //when words are legal
                        if (otherPlayer.Words.Contains(guess))
                        {
                            _commonWords.Add(guess);
                            player.Words.Remove(guess);
                            otherPlayer.Words.Remove(guess);
                            DeductPoints(otherPlayer, guess); //deduct point based on word length

                            ShowUpdatedScoreToClients(player, otherPlayer);
                        }
                        else
                        {
                            //need to add points based on word length

                            AddPoints(player, guess);
                            player.Words.Add(guess);
                            ShowUpdatedScoreToClients(player, otherPlayer);

                        }
                    }
                }
                else
                {
                    TreatIllegalWords(player, guess);
                    ShowUpdatedScoreToClients(player, otherPlayer);
                }
            }

        }

        /// <summary>
        /// Helper Method
        /// -Sends Updated Score to both clients
        /// </summary>
        private static void ShowUpdatedScoreToClients(Player player, Player otherPlayer)
        {
            SendMessage(player, "SCORE " + player.Score + " " + otherPlayer.Score + "\n");
            SendMessage(otherPlayer, "SCORE " + otherPlayer.Score + " " + player.Score + "\n");
        }

        /// <summary>
        /// Helper Method
        /// -Deducts a point from the player and add a word to the IllegalWords List
        /// -Removes the word from the list of valid words guessed
        /// </summary>
        private void TreatIllegalWords(Player player, string guess)
        {
            DeductPoints(player, guess);
            player.IllegalWords.Add(guess);
            player.Words.Remove(guess);
        }

        /// <summary>
        /// Helper Method
        /// -Calculate points for word guess based on word length.
        /// </summary>
        private void AddPoints(Player player, string word)
        {
            switch (word.Length)
            {
                case 3:
                case 4:
                    player.Score += 1;
                    break;
                case 5:
                    player.Score += 2;
                    break;
                case 6:
                    player.Score += 3;
                    break;
                case 7:
                    player.Score += 5;
                    break;
                default:
                    player.Score += 11;
                    break;
            }
        }

        /// <summary>
        /// Helper Method
        /// -Deducts a points from the player's score.
        /// </summary>
        private void DeductPoints(Player player, string word)
        {
            player.Score--;
        }
    }
}
