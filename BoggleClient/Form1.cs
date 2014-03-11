//Written by Minwen Gao and Zach Lobato - CS3500 - Fall 2013 - PS10

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoggleClient
{
    public partial class Form1 : Form
    {
        private BoggleClientModel model;

        public Form1()
        {
            InitializeComponent();
            model = new BoggleClientModel();
            model.IncomingLineEvent += StringReceived;
        }

        //Connect to the server
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                ConnectToServer();
            });
        }

        //connect to localhost if no IP address was entered,
        //otherwise, use the ip in ip address textbox
        private void ConnectToServer()
        {
            if (textBox2.Text.Trim().Equals(""))
            {
                model.Connect(2000, "localhost");
                model.IsConnected = true;
            }
            else
            {
                try
                {
                    model.Connect(2000, textBox2.Text);
                }
                catch (Exception)
                {
                    txtBoxOutput.Invoke(new Action(() =>
                    {
                        txtBoxOutput.Text = "Could not connect to Server. Please verify the IP Address and try again.";
                    }));
                }
            }

            //enable buttons once the current board connects to the server
            txtBoxName.Invoke(new Action(() =>
            {
                txtBoxName.Focus();
            }));

            btnPlayNew.Invoke(new Action(() =>
            {
                btnPlayNew.Enabled = true;
            }));

            btnConnect.Invoke(new Action(() =>
            {
                btnConnect.Enabled = false;
            }));

            btnDisconnect.Invoke(new Action(() =>
            {
                btnDisconnect.Enabled = true;
            }));

            btnEndGame.Invoke(new Action(() =>
            {
                btnEndGame.Enabled = false;
            }));

            if (txtBoxName.Text.Equals(""))
            {
                btnPlayNew.Invoke(new Action(() =>
                {
                    btnPlayNew.Enabled = false;
                }));
            }
        }

        //Disconnect the current player
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            DisconnectFromServer();
        }

        //disable buttons (except Connect button) when player
        //click on the Disconnect button
        private void DisconnectFromServer()
        {
            model.Disconnect();

            btnPlayNew.Invoke(new Action(() =>
            {
                btnPlayNew.Enabled = false;
            }));

            btnConnect.Invoke(new Action(() =>
            {
                btnConnect.Enabled = true;
            }));

            btnDisconnect.Invoke(new Action(() =>
            {
                btnDisconnect.Enabled = false;
            }));
        }

        //Disconnects the player, and enable the connect button
        private void btnEndGame_Click(object sender, EventArgs e)
        {
            DisconnectFromServer();
            ConnectToServer();
        }


        private void SubmitGuess()
        {
            model.SendMessage(txtBoxEnterWord.Text.Trim());
            txtBoxEnterWord.Text = "";
        }

        //sends the msg in the textbox to the server
        //empty the textbox
        private void btnSubmitGuess_Click(object sender, EventArgs e)
        {
            SubmitGuess();
            txtBoxEnterWord.Focus();
        }

        //Enable the Enter key to enter word to the server
        private void txtBoxEnterWord_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SubmitGuess();
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        //analize different messages players receive
        private void StringReceived(string msgReceived)
        {
            int count = 0;
            if (msgReceived != null)
            {
                string[] msgAsArray = Regex.Split(msgReceived.Trim(), @"\s+", RegexOptions.IgnorePatternWhitespace);

                switch (msgAsArray[0])
                {
                    case "START":
                        StartTheGame(msgAsArray);
                        break;
                    case "STOP":
                        Summary(msgAsArray);
                        count++;
                        break;
                    case "TERMINATED":
                        Terminated();
                        break;
                    case "TIME":
                        TimeUpdater(msgAsArray);
                        break;
                    case "IGNORING":
                        break;
                    case "SCORE":
                        UpdateScore(msgAsArray);
                        break;
                    default:
                        Console.WriteLine("Default case");
                        break;
                }
                if (count == 2)
                {
                    model.Disconnect();
                }
            }
        }

        //sends a summary of the game to player
        //when game ends smoothly
        private void Summary(string[] msgAsArray)
        {
            /*"STOP " 
             *  + currentGame.PlayerA.Words.Count + " " + aALegalWords + " "
                + currentGame.PlayerB.Words.Count + " " + bBLegalWords + " "
                + currentGame.CommonWords.Count + " " + cCommonWords + " "
                + currentGame.PlayerA.IllegalWords.Count + " " + dAIllegalWords + " "
                + currentGame.PlayerB.IllegalWords.Count + " " + eBIllegalWords + " " + "\r\n");
             * */
            // A Legal Words
                int stops = 0;
                int PALegalWordsCount;
                
                Int32.TryParse(msgAsArray[1 + stops], out PALegalWordsCount);
                StringBuilder ALegalWords = new StringBuilder("");

                if (PALegalWordsCount > 0)
                {
                    for (int i = 0; i < PALegalWordsCount; i++)
                    {
                        ALegalWords.Append(msgAsArray[2 + stops + i] + " ");
                    }
                }

                stops += PALegalWordsCount;

            // B Legal Words
                int PBLegalWordsCount;

                Int32.TryParse(msgAsArray[2 + stops], out PBLegalWordsCount);
                StringBuilder BLegalWords = new StringBuilder("");

                if (PBLegalWordsCount > 0)
                {
                    for (int i = 0; i < PBLegalWordsCount; i++)
                    {
                       BLegalWords.Append(msgAsArray[3 + stops + i] + " ");
                    }
                }

                stops += PBLegalWordsCount;

            // Common words
                int commonWordsCount;
                
                Int32.TryParse(msgAsArray[3 + stops], out commonWordsCount);

                StringBuilder commonWords = new StringBuilder("");

                if (commonWordsCount > 0)
                {
                    for (int i = 0; i < commonWordsCount; i++)
                    {
                        commonWords.Append(msgAsArray[4 + stops + i] + " ");
                    }
                }

                stops += commonWordsCount;

            // A illegal words
                int PAIllegalWordsCount;

                Int32.TryParse(msgAsArray[4 + stops], out PAIllegalWordsCount);
                StringBuilder PAIllegalWords = new StringBuilder("");

                if (PAIllegalWordsCount > 0)
                {
                    for (int i = 0; i < PAIllegalWordsCount; i++)
                    {
                        PAIllegalWords.Append(msgAsArray[5 + stops + i] + " ");
                    }
                }

                stops += PAIllegalWordsCount;

            // B illegal words
                int PBIllegalWordsCount;

                Int32.TryParse(msgAsArray[5 + stops], out PBIllegalWordsCount);
                StringBuilder PBIllegalWords = new StringBuilder("");

                if (PBIllegalWordsCount > 0)
                {
                    for (int i = 0; i < PBIllegalWordsCount; i++)
                    {
                        PBIllegalWords.Append(msgAsArray[6 + stops + i] + " ");
                    }
                }

                stops += PBIllegalWordsCount;

            txtBoxOutput.Invoke(new Action(() =>
            {
                txtBoxOutput.Text = "You played " + PALegalWordsCount.ToString() + " Legal Words: " + ALegalWords.ToString() + "\r\n" +
                                    "Your opponent Played " + PBLegalWordsCount.ToString() + " Legal Words:  " + BLegalWords.ToString() + "\r\n" +
                                    "You had " + commonWordsCount.ToString() + " Words In Common:  " + commonWords.ToString() + "\r\n" +
                                    "You played " + PAIllegalWordsCount.ToString() + " Illegal Words: " + PAIllegalWords.ToString() + "\r\n" +
                                    "Your opponent played " + PBIllegalWordsCount.ToString() + " Illegal Words: " + PBIllegalWords.ToString() + "\r\n" +
                                    "Click \"New Game\" to play again.";
            }));
            model.IsConnected = false;
            DisconnectFromServer();
            ConnectToServer();
        }

        //Display the terminate message if another player disconnects
        //Let the connected player know they can play again
        private void Terminated()
        {
            txtBoxOutput.Invoke(new Action(() =>
            {
                txtBoxOutput.Text = "The Opponent has disconnected. Click \"New Game\" to play again.";
            }));
            DisconnectFromServer();
            ConnectToServer();
        }

        //Updates the time on the board
        private void TimeUpdater(string[] msgReceived)
        {
            if (model.IsConnected)
            {
                lblTime.Invoke(new Action(() =>
                {
                    lblTime.Text = "Time: " + msgReceived[1].ToString();
                }));
            }
        }

        //Update the score on the board
        private void UpdateScore(string[] msgReceived)
        {
            if (model.IsConnected)
            {
                lblScore.Invoke(new Action(() =>
                {
                    lblScore.Text = msgReceived[1].ToString();
                }));

                lblOpponentScore.Invoke(new Action(() =>
                {
                    lblOpponentScore.Text = msgReceived[2].ToString();
                }));
            }
        }

        //Starts a game, updates characters on the board buttons, and
        //everything the player needs to know about the game
        private void StartTheGame(string[] msgReceived)
        {
            string board = msgReceived[1];
            Random randomGen = new Random();

            // Update the Duration
            int dur;
            Int32.TryParse(msgReceived[2], out dur);
            UpdateDuration(dur);

            // Update the Player Names
            UpdateNames(msgReceived[3]);

            //Update Button Names
            btn1.Invoke(new Action(() =>
            {
                btn1.Tag = board[0].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[0].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn1, randomGen, 17, 22);
                btn1.BackgroundImage = btnBit;
            }));

            btn2.Invoke(new Action(() =>
            {
                btn2.Tag = board[1].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[1].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn2, randomGen, 108, 22);
                btn2.BackgroundImage = btnBit;

            }));

            btn3.Invoke(new Action(() =>
            {
                btn3.Tag = board[2].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[2].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn3, randomGen, 199, 22);
                btn3.BackgroundImage = btnBit;
            }));

            btn4.Invoke(new Action(() =>
            {
                btn4.Tag = board[3].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[3].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn4, randomGen, 290, 22);
                btn4.BackgroundImage = btnBit;
            }));

            btn5.Invoke(new Action(() =>
            {
                btn5.Tag = board[4].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[4].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn5, randomGen, 17, 114);
                btn5.BackgroundImage = btnBit;
            }));

            btn6.Invoke(new Action(() =>
            {
                btn6.Tag = board[5].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[5].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn6, randomGen, 108, 114);
                btn6.BackgroundImage = btnBit;
            }));

            btn7.Invoke(new Action(() =>
            {
                btn7.Tag = board[6].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[6].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn7, randomGen, 199, 114);
                btn7.BackgroundImage = btnBit;
            }));

            btn8.Invoke(new Action(() =>
            {
                btn8.Tag = board[7].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[7].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn8, randomGen, 290, 114);
                btn8.BackgroundImage = btnBit;
            }));


            btn9.Invoke(new Action(() =>
            {
                btn9.Tag = board[8].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[8].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn9, randomGen, 17, 206);
                btn9.BackgroundImage = btnBit;
            }));

            btn10.Invoke(new Action(() =>
            {
                btn10.Tag = board[9].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[9].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn10, randomGen, 108, 206);
                btn10.BackgroundImage = btnBit;
            }));

            btn11.Invoke(new Action(() =>
            {
                btn11.Tag = board[10].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[10].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn11, randomGen, 199, 206);
                btn11.BackgroundImage = btnBit;
            }));

            btn12.Invoke(new Action(() =>
            {
                btn12.Tag = board[11].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[11].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn12, randomGen, 290, 206);
                btn12.BackgroundImage = btnBit;
            }));

            btn13.Invoke(new Action(() =>
            {
                btn13.Tag = board[12].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[12].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn13, randomGen, 17, 298);
                btn13.BackgroundImage = btnBit;
            }));

            btn14.Invoke(new Action(() =>
            {
                btn14.Tag = board[13].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[13].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn14, randomGen, 108, 298);
                btn14.BackgroundImage = btnBit;
            }));

            btn15.Invoke(new Action(() =>
            {
                btn15.Tag = board[14].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[14].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn15, randomGen, 199, 298);
                btn15.BackgroundImage = btnBit;
            }));

            btn16.Invoke(new Action(() =>
            {
                btn16.Tag = board[15].ToString();
                Bitmap btnBit = new Bitmap("..\\..\\..\\BoggleClient\\Resources\\" + board[15].ToString() + ".png");
                RotateRandomly(btnBit, randomGen);
                ShiftRandomly(btn16, randomGen, 290, 298);
                btn16.BackgroundImage = btnBit;
            }));
        }

        private void ShiftRandomly(Button btn, Random shift, int p1, int p2)
        {
            btn.Location = new Point(p1 + shift.Next(-4, 5), p2 + shift.Next(-4, 5));
        }

        //Rotates the characters on the board randomly
        private static void RotateRandomly(Bitmap btnBit, Random rotate)
        {
            switch (rotate.Next(1,5))
            {
                case 1:
                    btnBit.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                case 2:
                    btnBit.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                case 3:
                    btnBit.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
                case 4:
                    break;
            }
        }

        //Updates players' names on the boards
        private void UpdateNames(string p2)
        {
            lblName.Invoke(new Action(() =>
            {
                lblName.Text = txtBoxName.Text.ToUpper();
            }));

            lblOpponentName.Invoke(new Action(() =>
            {
                lblOpponentName.Text = p2;
            }));
        }

        //Updates time duration
        private void UpdateDuration(int dur)
        {
            lblTime.Invoke(new Action(() =>
            {
                lblTime.Text = "Time: " + dur.ToString();
            }));
        }

        private void TextBoxFocus()
        {
            txtBoxEnterWord.Focus();
            txtBoxEnterWord.SelectionStart = txtBoxEnterWord.Text.Length + 1;
        }

        private void btn1_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn1.Tag;
            TextBoxFocus();
        }

        private void btn2_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn2.Tag;
            TextBoxFocus();
        }

        private void btn3_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn3.Tag;
            TextBoxFocus();
        }

        private void btn4_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn4.Tag;
            TextBoxFocus();
        }

        private void btn5_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn5.Tag;
            TextBoxFocus();
        }

        private void btn6_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn6.Tag;
            TextBoxFocus();
        }

        private void btn7_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn7.Tag;
            TextBoxFocus();
        }

        private void btn8_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn8.Tag;
            TextBoxFocus();
        }

        private void btn9_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn9.Tag;
            TextBoxFocus();
        }

        private void btn10_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn10.Tag;
            TextBoxFocus();
        }

        private void btn11_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn11.Tag;
            TextBoxFocus();
        }

        private void btn12_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn12.Tag;
            TextBoxFocus();
        }

        private void btn13_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn13.Tag;
            TextBoxFocus();
        }

        private void btn14_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn14.Tag;
            TextBoxFocus();
        }

        private void btn15_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn15.Tag;
            TextBoxFocus();
        }

        private void btn16_Click(object sender, EventArgs e)
        {
            txtBoxEnterWord.Text += btn16.Tag;
            TextBoxFocus();
        }

        //Disconnects the board if the form is closed
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            model.Disconnect();
        }

        //Start the game, display 0's for scores
        //Enables End Game button
        private void btnPlay_Click(object sender, EventArgs e)
        {
            model.Play(txtBoxName.Text);
            TextBoxFocus();
            btnEndGame.Enabled = true;
            lblScore.Text = "0";
            lblOpponentScore.Text = "0";
            txtBoxOutput.Text = "";
        }

        

        private void txtBoxName_TextChanged(object sender, EventArgs e)
        {
            if (txtBoxName.Text.Equals(""))
            {
                btnPlayNew.Invoke(new Action(() =>
                {
                    btnPlayNew.Enabled = false;
                }));
            }
            else
            {
                btnPlayNew.Invoke(new Action(() =>
                {
                    btnPlayNew.Enabled = true;
                }));
            }
        }

        
    }
}
