using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;

namespace GameCaro
{
    public partial class Form1 : Form
    {
        #region Properties

        private ChessBoardManager ChessBoard;

        private SocketManager socket;

        #endregion Properties

        public Form1( )
        {
            InitializeComponent( );
            //panelChessBoard.Enabled = false;

            Control.CheckForIllegalCrossThreadCalls = false;

            ChessBoard = new ChessBoardManager( panelChessBoard , txtPlayerName , pcbMark );
            ChessBoard.EndedGame += ChessBoard_EndedGame;
            ChessBoard.PlayerMarked += ChessBoard_PlayerMarked;

            pcbCoolDown.Step = Cons.COOL_DOWN_STEP;
            pcbCoolDown.Maximum = Cons.COOL_DOWN_TIME;
            pcbCoolDown.Value = 0;

            tmCoolDown.Interval = Cons.COOL_DOWN_INTERVAL;
            socket = new SocketManager( );

            NewGame( );
        }

        #region Methods

        public void EndGame( )
        {
            tmCoolDown.Stop( );
            panelChessBoard.Enabled = false;
            undoToolStripMenuItem.Enabled = false;
            //MessageBox.Show( "End Game" );
        }

        private void NewGame( )
        {
            pcbCoolDown.Value = 0;
            tmCoolDown.Stop( );
            undoToolStripMenuItem.Enabled = true;

            ChessBoard.DrawChessBoard( );
        }

        private void Quit( )
        {
            Application.Exit( );
        }

        private void Undo( )
        {
            ChessBoard.Undo( );
            pcbCoolDown.Value = 0;
        }

        private void ChessBoard_PlayerMarked( object sender , ButtonClickEvent e )
        {
            tmCoolDown.Start( );
            panelChessBoard.Enabled = false;
            pcbCoolDown.Value = 0;

            socket.Send( new SocketData( ( int ) SocketCommand.SEND_POINT , "" , point: e.ClickedPoint ) );

            undoToolStripMenuItem.Enabled = false;

            Listen( );
        }

        private void ChessBoard_EndedGame( object sender , EventArgs e )
        {
            EndGame( );
            socket.Send( new SocketData( ( int ) SocketCommand.END_GAME , "" , point: new Point( ) ) );
        }

        private void tmCoolDown_Tick( object sender , EventArgs e )
        {
            pcbCoolDown.PerformStep( );

            if ( pcbCoolDown.Value >= pcbCoolDown.Maximum )
            {
                EndGame( );
                socket.Send( new SocketData( ( int ) SocketCommand.TIME_OUT , "" , point: new Point( ) ) );
            }
        }

        private void newGameToolStripMenuItem_Click( object sender , EventArgs e )
        {
            NewGame( );
            socket.Send( new SocketData( ( int ) SocketCommand.NEW_GAME , "" , point: new Point( ) ) );
            panelChessBoard.Enabled = true;
        }

        private void undoToolStripMenuItem_Click( object sender , EventArgs e )
        {
            Undo( );
            socket.Send( new SocketData( ( int ) SocketCommand.UNDO , "" , point: new Point( ) ) );
        }

        private void quitToolStripMenuItem_Click( object sender , EventArgs e )
        {
            Quit( );
        }

        private void Form1_FormClosing( object sender , FormClosingEventArgs e )
        {
            if ( MessageBox.Show( "Are you sure you want to Quit ?" , "Warning" , MessageBoxButtons.OKCancel ) != System.Windows.Forms.DialogResult.OK )
                e.Cancel = true;
            else
            {
                try
                {
                    socket.Send( new SocketData( ( int ) SocketCommand.QUIT , "" , point: new Point( ) ) );
                }
                catch { }
            }
        }

        private void btnLan_Click( object sender , EventArgs e )
        {
            socket.IP = txtIP.Text;

            if ( !socket.ConnectServer( ) )
            {
                socket.isServer = true;
                panelChessBoard.Enabled = true;
                socket.CreateServer( );
            }
            else
            {
                socket.isServer = false;
                panelChessBoard.Enabled = false;
                Listen( );
            }
        }

        private void Form1_Shown( object sender , EventArgs e )
        {
            txtIP.Text = socket.GetLocalIPv4( NetworkInterfaceType.Wireless80211 );

            if ( string.IsNullOrEmpty( txtIP.Text ) )
            {
                txtIP.Text = socket.GetLocalIPv4( NetworkInterfaceType.Ethernet );
            }
        }

        private void Listen( )
        {
            Thread listenThread = new Thread(()=>
                {
                    try
                    {
                        SocketData data = (SocketData)socket.Receive();

                        ProcessData(data);
                    }
                    catch (Exception e)
                    {
                    }
                } );
            listenThread.IsBackground = true;
            listenThread.Start( );
        }

        private void ProcessData( SocketData data )
        {
            switch ( data.Command )
            {
                case ( int ) SocketCommand.NOTIFY:
                    MessageBox.Show( data.Message );
                    break;

                case ( int ) SocketCommand.NEW_GAME:
                    this.Invoke( ( MethodInvoker ) ( ( ) =>
                       {
                           NewGame( );
                           panelChessBoard.Enabled = false;
                       } ) );

                    break;

                case ( int ) SocketCommand.SEND_POINT:
                    this.Invoke( ( MethodInvoker ) ( ( ) =>
                         {
                             pcbCoolDown.Value = 0;
                             panelChessBoard.Enabled = true;
                             tmCoolDown.Start( );
                             ChessBoard.OtherPlayerMark( data.Point );
                             undoToolStripMenuItem.Enabled = true;
                         } ) );
                    break;

                case ( int ) SocketCommand.UNDO:
                    Undo( );
                    pcbCoolDown.Value = 0;
                    break;

                case ( int ) SocketCommand.END_GAME:
                    MessageBox.Show( data.Message + "\nGame End" );
                    break;

                case ( int ) SocketCommand.TIME_OUT:
                    MessageBox.Show( "Timer Run Out" );
                    break;

                case ( int ) SocketCommand.QUIT:
                    tmCoolDown.Stop( );
                    MessageBox.Show( "Player Quit" );
                    break;

                default:
                    break;
            }

            Listen( );
        }

        #endregion Methods
    }
}