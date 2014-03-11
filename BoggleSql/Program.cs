//Written by Minwen Gao and Zach Lobato - CS3500 - Fall 2013 - PS10
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace BoggleSql
{
    public class Program
    {
        /// <summary>
        /// The connection string.
        /// Your CADE login name serves as both your database name and your uid
        /// Your uNID serves as your password
        /// </summary>
        public const string connectionString = "server=atr.eng.utah.edu;database=minweng;uid=minweng;password=462415881";

        /// <summary>
        ///  Test several connections and print the output to the console
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            AllPlayers();
            AllGameInfo();
            Console.ReadLine();
        }

        /// <summary>
        /// Helper Method - to Query for All the Players
        /// </summary>
        public static void AllPlayers()
        {
            //Connect to the DB
            using (MySqlConnection connect = new MySqlConnection(connectionString))
            {
                try
                {
                    connect.Open();

                    //Create a command
                    MySqlCommand command = connect.CreateCommand();
                    command.CommandText = "select PlayerID, PlayerName, GameID from Players";
                    using (MySqlDataReader read = command.ExecuteReader())
                    {
                        while (read.Read())
                        {
                            Console.WriteLine("PlayerID" + "\t" + "PlayerName" + "\t" + "GameID");
                            Console.WriteLine(read["PlayerID"] + "\t\t" + read["PlayerName"] + "\t\t" + read["GameID"]);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Helper Method - to Query the whole Game
        /// </summary>
        public static void AllGameInfo()
        {
            //Connect to the DB
            using (MySqlConnection connect = new MySqlConnection(connectionString))
            {
                try
                {
                    connect.Open();

                    //create a command
                    MySqlCommand command = connect.CreateCommand();
                    command.CommandText = "select GameID, EndDate, EndTime, TimeLimit, Board from GameInfo";

                    using (MySqlDataReader read = command.ExecuteReader())
                    {
                        while (read.Read())
                        {
                            Console.WriteLine("GameID" + " \t" + "EndDate" + " \t" + "EndTime" + "\t\t" + "TimeLimit" + "\t" + "Board");
                            Console.WriteLine(read["GameID"] + "\t" + read["EndDate"] + "\t" + read["EndTime"] + "\t" + read["TimeLimit"] + "\t\t" + read["Board"]);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Static Method - Allows to Write to Entire Database
        /// </summary>
        public static void WritingToGameInfoDB(string playerAName, string playerBName, int playerAScore, int playerBScore, string playerASummary, string playerBSummary,
            string endDateTime, int timeLimit, string board)
        {

            //Connect to the DB
            using (MySqlConnection connect = new MySqlConnection(connectionString))
            {
                try
                {
                    connect.Open();

                    //create a command
                    using (MySqlCommand command = connect.CreateCommand())
                    {

                        //check if player A and B's names exist, catch exception if they do. 
                        try
                        {
                            command.CommandText = "insert into Players (PlayerName) values (@playerAName);";

                            command.Parameters.AddWithValue("@playerAName", playerAName);

                            command.Prepare();

                            command.ExecuteNonQuery();
                        }

                        catch
                        { }

                        try
                        {
                            command.CommandText = "insert into Players (PlayerName) values (@playerBName);";

                            command.Parameters.AddWithValue("@playerBName", playerBName);

                            command.Prepare();

                            command.ExecuteNonQuery();
                        }
                        catch
                        { }

                        //adding player A ID by getting the ID from Players table
                        command.CommandText = @"select PlayerID from Players where Players.PlayerName = @playerAName;";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@playerAName", playerAName);
                        MySqlDataReader reader;
                        int playerAID = 0;
                        using (reader = command.ExecuteReader())
                        {
                            //int playerAID = int.Parse(reader["PlayerID"].ToString());
                            if (reader.Read())
                            {
                                playerAID = reader.GetInt32("PlayerID");
                            }
                        }
                        //adding player B ID by getting the ID from Players table
                        command.CommandText = @"select PlayerID from Players where Players.PlayerName = @playerBName;";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@playerBName", playerBName);
                        int playerBID = 0;
                        using (reader = command.ExecuteReader())
                        {
                            //int playerBID = int.Parse(reader["PlayerID"].ToString());

                            if (reader.Read())
                            {
                                playerBID = reader.GetInt32("PlayerID");

                            }
                        }
                        if (playerAScore > playerBScore)
                        {
                            GeneralGameInfo(playerAID, playerBID, false,playerAScore, playerBScore, playerASummary, playerBSummary, endDateTime, timeLimit, board, command, playerAID, playerBID);
                        }
                        else if (playerBScore > playerAScore)
                        {
                            GeneralGameInfo(playerBID, playerAID, false, playerAScore, playerBScore, playerASummary, playerBSummary, endDateTime, timeLimit, board, command, playerAID, playerBID);
                        }
                        else
                        {
                            GeneralGameInfo(true, playerAScore, playerBScore, playerASummary, playerBSummary, endDateTime, timeLimit, board, command, playerAID, playerBID);
                        }

                        //put data into WordsPlayed table in the database

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Private Helper Method - To Write When Game Has Winner and Loser
        /// </summary>
        private static void GeneralGameInfo(int WinnerID, int LoserID, bool tied, int playerAScore, int playerBScore, string playerASummary, string playerBSummary, string endDateTime, int timeLimit, string board, MySqlCommand command, int playerAID, int playerBID)
        {
            //insert information into GameInfo table after each game
            command.CommandText = "insert into GameInfo (WinnerName, LoserName, Tied) values(@playerAID, @playerBID, 0)";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@playerAID", playerAID);
            command.Parameters.AddWithValue("@playerBID", playerBID);
            command.CommandText = @"insert into GameInfo (EndDateTime, TimeLimit, Board, PlayerAID, ScoreA, SummaryA, PlayerBID, ScoreB, SummaryB, WinnerName, LoserName, Tied) values (@endDateTime, @timeLimit, @board, @playerAID, @playerAScore,
                        @playerASummary, @playerBID, @playerBScore, @playerBSummary, @winnerID, @loserID, @tied);";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@endDateTime", endDateTime);
            command.Parameters.AddWithValue("@timeLimit", timeLimit);
            command.Parameters.AddWithValue("@board", board);
            command.Parameters.AddWithValue("@playerAScore", playerAScore);
            command.Parameters.AddWithValue("@playerASummary", playerASummary);
            command.Parameters.AddWithValue("@playerBScore", playerBScore);
            command.Parameters.AddWithValue("@playerBSummary", playerBSummary);
            command.Parameters.AddWithValue("@playerAID", playerAID.ToString());
            command.Parameters.AddWithValue("@playerBID", playerBID.ToString());
            command.Parameters.AddWithValue("@winnerID", WinnerID.ToString());
            command.Parameters.AddWithValue("@loserID", LoserID.ToString());
            command.Parameters.AddWithValue("tied", 0);
            command.Prepare();
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Private Helper Method - To Write When Game Is Tied
        /// </summary>
        private static void GeneralGameInfo(bool tied, int playerAScore, int playerBScore, string playerASummary, string playerBSummary, string endDateTime, int timeLimit, string board, MySqlCommand command, int playerAID, int playerBID)
        {
            //insert information into GameInfo table after each game
            command.CommandText = @"insert into GameInfo (EndDateTime, TimeLimit, Board, PlayerAID, ScoreA, SummaryA, PlayerBID, ScoreB, SummaryB, Tied) values (@endDateTime, @timeLimit, @board, @playerAID, @playerAScore,
                        @playerASummary, @playerBID, @playerBScore, @playerBSummary, @tied);";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@endDateTime", endDateTime);
            command.Parameters.AddWithValue("@timeLimit", timeLimit);
            command.Parameters.AddWithValue("@board", board);
            command.Parameters.AddWithValue("@playerAScore", playerAScore);
            command.Parameters.AddWithValue("@playerASummary", playerASummary);
            command.Parameters.AddWithValue("@playerBScore", playerBScore);
            command.Parameters.AddWithValue("@playerBSummary", playerBSummary);
            command.Parameters.AddWithValue("@playerAID", playerAID.ToString());
            command.Parameters.AddWithValue("@playerBID", playerBID.ToString());
            command.Parameters.AddWithValue("@tied", 1);
            command.Prepare();
            command.ExecuteNonQuery();
        }
    }
}