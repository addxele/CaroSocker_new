﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameCaro
{
    public class ChessBoardManager
    {
        #region Properties

        private Panel chessBoard;

        public Panel ChessBoard
        {
            get => chessBoard;
            set => chessBoard = value;
        }

        private List<Player> player;

        public List<Player> Player
        {
            get => player;
            set => player = value;
        }

        private int currentPlayer;

        public int CurrentPlayer
        {
            get => currentPlayer;
            set => currentPlayer = value;
        }

        private TextBox playerName;

        public TextBox PlayerName
        {
            get => playerName;
            set => playerName = value;
        }

        private PictureBox playerMark;

        public PictureBox PlayerMark
        {
            get => playerMark;
            set => playerMark = value;
        }

        private List<List<Button>> matrix;

        public List<List<Button>> Matrix
        {
            get => matrix;
            set => matrix = value;
        }

        private event EventHandler<ButtonClickEvent> playerMarked;

        public event EventHandler<ButtonClickEvent> PlayerMarked
        {
            add
            {
                playerMarked += value;
            }
            remove
            {
                playerMarked -= value;
            }
        }

        private event EventHandler endedGame;

        public event EventHandler EndedGame
        {
            add
            {
                endedGame += value;
            }
            remove
            {
                endedGame -= value;
            }
        }

        private Stack<PlayInfo> playTimeline;

        public Stack<PlayInfo> PlayTimeline
        {
            get => playTimeline;
            set => playTimeline = value;
        }

        #endregion Properties

        #region Initialize

        public ChessBoardManager( Panel chessBoard , TextBox playerName , PictureBox mark )
        {
            this.ChessBoard = chessBoard;
            this.PlayerName = playerName;
            this.PlayerMark = mark;
            this.Player = new List<Player>( )
            {
                new Player ("Player1", Image.FromFile(Application.StartupPath + "\\Resources\\O.png" )),
                new Player ("Player2", Image.FromFile(Application.StartupPath + "\\Resources\\X.png"))
            };
        }

        #endregion Initialize

        #region Method

        public void DrawChessBoard( )
        {
            CurrentPlayer = 0;

            ChangePlayer( );

            ChessBoard.Enabled = false;
            ChessBoard.Controls.Clear( );

            PlayTimeline = new Stack<PlayInfo>( );

            Matrix = new List<List<Button>>( );

            Button oldButton = new Button() {Width =0, Location = new Point(0,0) };

            for ( int i = 0 ; i < Cons.CHESS_BOARD_HEIGHT ; ++i )
            {
                Matrix.Add( new List<Button>( ) );
                for ( int j = 0 ; j < Cons.CHESS_BOARD_WIDTH ; ++j )
                {
                    Button btn = new Button()
                    {
                        Width = Cons.CHESS_WIDTH,
                        Height = Cons.CHESS_HEIGHT,
                        Location = new Point(oldButton.Location.X + oldButton.Width, oldButton.Location.Y),
                        BackgroundImageLayout = ImageLayout.Stretch,
                        Tag = i.ToString()
                    };

                    btn.Click += btn_Click;

                    ChessBoard.Controls.Add( btn );

                    Matrix[ i ].Add( btn );

                    oldButton = btn;
                }
                oldButton.Location = new Point( 0 , oldButton.Location.Y + Cons.CHESS_HEIGHT );
                oldButton.Width = 0;
                oldButton.Height = 0;
            }
        }

        private void btn_Click( object sender , EventArgs e )
        {
            Button btn = sender as Button;

            if ( btn.BackgroundImage != null )
                return;

            Mark( btn );

            PlayTimeline.Push( new PlayInfo( GetChessPoint( btn ) , CurrentPlayer ) );

            CurrentPlayer = CurrentPlayer == 1 ? 0 : 1;

            ChangePlayer( );

            if ( playerMarked != null )
                playerMarked( this , new ButtonClickEvent( GetChessPoint( btn ) , Player[ currentPlayer ] ) );

            if ( isEndGame( btn ) )
            {
                EndGame( );
            }
        }

        public void OtherPlayerMark( Point point )
        {
            Button btn = Matrix[point.Y][point.X];

            if ( btn.BackgroundImage != null )
                return;

            Mark( btn );

            PlayTimeline.Push( new PlayInfo( GetChessPoint( btn ) , CurrentPlayer ) );

            CurrentPlayer = CurrentPlayer == 1 ? 0 : 1;

            ChangePlayer( );

            if ( isEndGame( btn ) )
            {
                EndGame( );
            }
        }

        public void EndGame( )
        {
            if ( endedGame != null )
                endedGame( this , new EventArgs( ) );
        }

        public bool Undo( )
        {
            if ( PlayTimeline.Count( ) <= 0 )
                return false;

            bool isUndo1 = UndoAStep( );
            bool isUndo2 = UndoAStep( );

            PlayInfo oldPoint = PlayTimeline.Peek();
            CurrentPlayer = oldPoint.CurrentPlayer == 1 ? 0 : 1;

            return isUndo1 && isUndo2;
        }

        private bool UndoAStep( )
        {
            if ( PlayTimeline.Count( ) <= 0 )
                return false;
            PlayInfo oldPoint = PlayTimeline.Pop();
            Button btn = Matrix[oldPoint.Point.Y][oldPoint.Point.X];

            btn.BackgroundImage = null;

            if ( PlayTimeline.Count( ) <= 0 )
                CurrentPlayer = 0;
            else
            {
                oldPoint = PlayTimeline.Peek( );
            }

            ChangePlayer( );

            return true;
        }

        private bool isEndGame( Button btn )
        {
            return ( isEndHorizontal( btn ) || isEndVertical( btn ) || isEndPrimary( btn ) || isEndSub( btn ) );
        }

        #region EndGame helper function

        private Point GetChessPoint( Button btn )
        {
            int vertical = Convert.ToInt32(btn.Tag);
            int horizontal = Matrix[vertical].IndexOf(btn);

            Point point = new Point(horizontal, vertical);

            return point;
        }

        private bool isEndHorizontal( Button btn )
        {
            Point point = GetChessPoint(btn);

            int countLeft = 0;
            for ( int i = point.X ; i >= 0 ; --i )
            {
                if ( Matrix[ point.Y ][ i ].BackgroundImage == btn.BackgroundImage )
                    ++countLeft;
                else
                    break;
            }

            int countRight = 0;
            for ( int i = point.X + 1 ; i < Cons.CHESS_BOARD_WIDTH ; ++i )
            {
                if ( Matrix[ point.Y ][ i ].BackgroundImage == btn.BackgroundImage )
                    ++countRight;
                else
                    break;
            }

            return countLeft + countRight == 5;
        }

        private bool isEndVertical( Button btn )
        {
            Point point = GetChessPoint(btn);

            int countTop = 0;
            for ( int i = point.Y ; i >= 0 ; --i )
            {
                if ( Matrix[ i ][ point.X ].BackgroundImage == btn.BackgroundImage )
                    ++countTop;
                else
                    break;
            }

            int countBottom = 0;
            for ( int i = point.Y + 1 ; i < Cons.CHESS_BOARD_HEIGHT ; ++i )
            {
                if ( Matrix[ i ][ point.X ].BackgroundImage == btn.BackgroundImage )
                    ++countBottom;
                else
                    break;
            }

            return countTop + countBottom == 5;
        }

        private bool isEndPrimary( Button btn )
        {
            Point point = GetChessPoint(btn);

            int countTop = 0;
            for ( int i = 0 ; i <= point.X ; ++i )
            {
                if ( point.Y - i < 0 || point.X - i < 0 )
                    break;
                if ( Matrix[ point.Y - i ][ point.X - i ].BackgroundImage == btn.BackgroundImage )
                    ++countTop;
                else
                    break;
            }

            int countBottom = 0;
            for ( int i = 1 ; i <= Cons.CHESS_BOARD_WIDTH - point.X ; ++i )
            {
                if ( point.Y + i >= Cons.CHESS_BOARD_HEIGHT || point.X + i >= Cons.CHESS_BOARD_WIDTH )
                    break;
                if ( Matrix[ point.Y + i ][ point.X + i ].BackgroundImage == btn.BackgroundImage )
                    ++countTop;
                else
                    break;
            }

            return countTop + countBottom == 5;
        }

        private bool isEndSub( Button btn )
        {
            Point point = GetChessPoint(btn);

            int countTop = 0;
            for ( int i = 0 ; i <= point.X ; ++i )
            {
                if ( point.Y - i < 0 || point.X + i > Cons.CHESS_BOARD_WIDTH )
                    break;
                if ( Matrix[ point.Y - i ][ point.X + i ].BackgroundImage == btn.BackgroundImage )
                    ++countTop;
                else
                    break;
            }

            int countBottom = 0;
            for ( int i = 1 ; i <= Cons.CHESS_BOARD_WIDTH - point.X ; ++i )
            {
                if ( point.Y + i >= Cons.CHESS_BOARD_HEIGHT || point.X - i < 0 )
                    break;
                if ( Matrix[ point.Y + i ][ point.X - i ].BackgroundImage == btn.BackgroundImage )
                    ++countTop;
                else
                    break;
            }

            return countTop + countBottom == 5;
        }

        #endregion EndGame helper function

        private void Mark( Button btn )
        {
            btn.BackgroundImage = Player[ CurrentPlayer ].Mark;
        }

        private void ChangePlayer( )
        {
            PlayerName.Text = Player[ CurrentPlayer ].Name;
            PlayerMark.Image = Player[ CurrentPlayer ].Mark;
        }

        #endregion Method
    }

    public class ButtonClickEvent : EventArgs
    {
        private string playerName;

        private Point clickedPoint;

        public Point ClickedPoint { get => clickedPoint; set => clickedPoint = value; }
        public string PlayerName { get => playerName; set => playerName = value; }

        public ButtonClickEvent( Point point , Player player )
        {
            this.ClickedPoint = point;
            this.PlayerName = player.Name;
        }
    }
}