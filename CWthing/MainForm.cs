using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Numerics;


namespace CWthing
{
    using cw;

    public partial class RennysThing : Form
    {
        // Really inconsistent variable declarations
        public int gamePID = 0;
        public IntPtr hProc;
        public IntPtr baseAddress = IntPtr.Zero;
        public Color defaultColor = Color.Black;
        public bool trainerOn = false;
        public Process gameProc;
        public Single playerSpeed = -1f;
        public int zmTeleportDistance = 150;
        public bool ammoFrozen;
        public int[] ammoVals = new int[6];
        public int[] maxAmmoVals = new int[6];
        public bool freezePlayer;
        public Vector3 frozenPlayerPos = Vector3.Zero;
        public Vector3 lastKnownPlayerPos = Vector3.Zero;
        public Vector3 updatedPlayerPos = Vector3.Zero;

        public Single xpModifier = 1.0f;
        public Single gunXpModifier = 1.0f;


        // Big thanks to JayKoZa2015 on UnKnoWnCheaTs for the following addresses and offsets.
        // Source will have these blanked, be sure to change them to their latest values!

        public IntPtr PlayerBase = (IntPtr)0x0;
        public IntPtr ZMXPScaleBase = (IntPtr)0x0;
        public IntPtr TimeScaleBase = (IntPtr)0x0;
        public IntPtr CMDBufferBase = (IntPtr)0x0;
        public IntPtr XPScaleZM = (IntPtr)0x0;
        public IntPtr GunXPScaleZM = (IntPtr)0x0;

        public IntPtr PlayerCompPtr, PlayerPedPtr, ZMGlobalBase, ZMBotBase, ZMBotListBase, ZMXPScalePtr;

        public const int PC_ArraySize_Offset = 0xB830;
               
        public const int PC_CurrentUsedWeaponID = 0x28;
        public const int PC_SetWeaponID = 0xB0; // +(1-5 * 0x40 for WP2 to WP6)
        public const int PC_InfraredVision = 0xE66; // (byte) On=0x10|Off=0x0
        public const int PC_GodMode = 0xE67; // (byte) On=0xA0|Off=0x20
        public const int PC_RapidFire1 = 0xE6C;
        public const int PC_RapidFire2 = 0xE80; 
        public const int PC_MaxAmmo = 0x1360; // +(1-5 * 0x8 for WP1 to WP6)
        public const int PC_Ammo = 0x13D4; // +(1-5 * 0x4 for WP1 to WP6)
        public const int PC_Points = 0x5CE4; 
        public const int PC_Name = 0x5BDA;
        public const int PC_RunSpeed = 0x5C30; 
        public const int PC_ClanTags = 0x605C; 

        public const int PP_ArraySize_Offset = 0x5F8;

        public const int PP_Health = 0x398;
        public const int PP_MaxHealth = 0x39C;
        public const int PP_Coords = 0x2D4; 
        public const int PP_Heading_Z = 0x34; 
        public const int PP_Heading_XY = 0x38; 

        public const int ZM_Global_ZombiesIgnoreAll = 0x14;

        public const int ZM_Global_ZMLeftCount = 0x3C;

        public const int ZM_Bot_List_Offset = 0x8;

        public const int ZM_Bot_ArraySize_Offset = 0x5F8;

        public const int ZM_Bot_Health = 0x398;
        public const int ZM_Bot_MaxHealth = 0x39C;
        public const int ZM_Bot_Coords = 0x2D4;

        public const int XPEP_Offset = 0x20;

        public const int XPUNK01_Offset = 0x24;
        public const int XPUNK02_Offset = 0x28;
        public const int XPUNK03_Offset = 0x2c;
        public const int XPGun_Offset = 0x30; 
        public const int XPUNK04_Offset = 0x34;
        public const int XPUNK05_Offset = 0x38;
        public const int XPUNK06_Offset = 0x3c;
        public const int XPUNK07_Offset = 0x40;
        public const int XPUNK08_Offset = 0x44;
        public const int XPUNK09_Offset = 0x48;
        public const int XPUNK10_Offset = 0x4C;

        public const int CMDBB_Exec = -0x1B;


        public RennysThing()
        {
            InitializeComponent();
        }

        private void btnOnOff_Click(object sender, EventArgs e)
        {
            // Basic toggle button that enables/disables the tool.
            trainerOn = !trainerOn;

            if (trainerOn)
            {
                btnOnOff.Text = "RUNNING";
                btnOnOff.ForeColor = Color.Green;
                ConsoleOut("Trainer Enabled");
            }
            else
            {
                btnOnOff.Text = "STOPPED";
                btnOnOff.ForeColor = Color.Red;
                ConsoleOut("Trainer Disabled");
            }
        }

        // Don't mind the generic control names and their relevant functions, was too lazy to change them every time.

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            // Updates playerSpeed and syncs the trackBar and numericUpDown, then writes the value to the player speed memory address.

            playerSpeed = (float)numericUpDown1.Value;
            trackBar1.Value = Convert.ToInt32(numericUpDown1.Value);

            cwapi.WriteProcessMemory(hProc, PlayerCompPtr + PC_RunSpeed, Convert.ToSingle(playerSpeed), 4, out _);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            // Updates playerSpeed and syncs the trackBar and numericUpDown, then writes the value to the player speed memory address.

            playerSpeed = trackBar1.Value;
            numericUpDown1.Value = trackBar1.Value;

            cwapi.WriteProcessMemory(hProc, PlayerCompPtr + PC_RunSpeed, Convert.ToSingle(playerSpeed), 4, out _);
        }

        private void RennysThing_Load(object sender, EventArgs e)
        {
            // Init with console messages

            ConsoleOut("Made by user OfficialRenny for UnKnoWnCheaTs");
            ConsoleOut("Don't forget to start the trainer once you're in a Zombies game!");
            if (!backgroundWorker1.IsBusy) backgroundWorker1.RunWorkerAsync();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            // Updates Zombie TP distance, syncs both the trackbar and numud

            zmTeleportDistance = trackBar2.Value;
            numericUpDown2.Value = Convert.ToInt32(trackBar2.Value);

        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            // Updates Zombie TP distance, syncs both the trackbar and numud

            zmTeleportDistance = Convert.ToInt32(numericUpDown2.Value);
            trackBar2.Value = Convert.ToInt32(numericUpDown2.Value);

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            // toggles the ammoFrozen bool

            ammoFrozen = checkBox3.Checked;
        }

        // Unusused trackbars and numuds, couldn't get the player position to update in my limited time of testing

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            numericUpDown3.Value = trackBar3.Value;

            frozenPlayerPos.X = (float)numericUpDown3.Value;
            UpdatePlayerPosition(frozenPlayerPos);
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {

            trackBar3.Value = Convert.ToInt32(numericUpDown3.Value);

            frozenPlayerPos.X = (float)numericUpDown3.Value;
            UpdatePlayerPosition(frozenPlayerPos);
        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            numericUpDown5.Value = trackBar5.Value;

            frozenPlayerPos.Y = (float)numericUpDown5.Value;
            UpdatePlayerPosition(frozenPlayerPos);
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            trackBar5.Value = Convert.ToInt32(numericUpDown5.Value);

            frozenPlayerPos.Y = (float)numericUpDown3.Value;
            UpdatePlayerPosition(frozenPlayerPos);
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            numericUpDown4.Value = trackBar4.Value;

            frozenPlayerPos.Z = (float)numericUpDown4.Value;
            UpdatePlayerPosition(frozenPlayerPos);
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            trackBar4.Value = Convert.ToInt32(numericUpDown4.Value);

            frozenPlayerPos.Z = (float)numericUpDown4.Value;
            UpdatePlayerPosition(frozenPlayerPos);
        }

        // Freeze player checkbox, unused, didnt really have any idea what I was trying to do in order to freeze the player and move them around with sliders.

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox2.Checked)
            {
                frozenPlayerPos = Vector3.Zero;
                freezePlayer = false;
            } else
            {
                frozenPlayerPos = updatedPlayerPos;
                freezePlayer = true;
            }
        }

        // Unused slider and numud end.


        // Backround worker function

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                try
                {
                    // if trainer isnt enabled, do nothing yet
                    if (!trainerOn) continue;

                    // get all processes called "BlackOpsColdWar"
                    var gameProcs = Process.GetProcessesByName("BlackOpsColdWar");

                    // if there aren't any processes, update the game message label and do nothing
                    if (gameProcs.Length < 1)
                    {
                        UpdateLabel(lblGameRunning, "Game is not running", "Red");
                        continue;
                    }

                    // get first process from the gameProcs array
                    gameProc = gameProcs[0];

                    // set gamePID to the Id of the gameProc
                    gamePID = gameProc.Id;

                    // update the label as needed, if for whatever reason the gamePID doesnt exist, update the label and do nothing
                    if (gamePID > 0)
                    {
                        UpdateLabel(lblGameRunning, "Game is running! Process ID: " + gamePID, "Green");
                    }
                    else
                    {
                        UpdateLabel(lblGameRunning, "Game is not running", "Red");
                        continue;
                    }

                    // opens the process or something, not 100% still learning all this terminology
                    hProc = cwapi.OpenProcess(cwapi.ProcessAccessFlags.All, false, gameProc.Id);

                    // if the base address isn't uptodate, update it
                    if (baseAddress != cwapi.GetModuleBaseAddress(gameProc, "BlackOpsColdWar.exe")) baseAddress = cwapi.GetModuleBaseAddress(gameProc, "BlackOpsColdWar.exe");

                    // cache the base addresses for these various pointers
                    if (PlayerCompPtr != cwapi.FindDMAAddy(hProc, (IntPtr)(baseAddress.ToInt64() + PlayerBase.ToInt64()), new int[] { 0 }))
                        PlayerCompPtr = cwapi.FindDMAAddy(hProc, (IntPtr)(baseAddress.ToInt64() + PlayerBase.ToInt64()), new int[] { 0 });

                    if (PlayerPedPtr != cwapi.FindDMAAddy(hProc, (IntPtr)(baseAddress.ToInt64() + PlayerBase.ToInt64() + 0x8), new int[] { 0 }))
                        PlayerPedPtr = cwapi.FindDMAAddy(hProc, (IntPtr)(baseAddress.ToInt64() + PlayerBase.ToInt64() + 0x8), new int[] { 0 });

                    if (ZMGlobalBase != cwapi.FindDMAAddy(hProc, (IntPtr)(baseAddress.ToInt64() + PlayerBase.ToInt64() + 0x60), new int[] { 0 }))
                        ZMGlobalBase = cwapi.FindDMAAddy(hProc, (IntPtr)(baseAddress.ToInt64() + PlayerBase.ToInt64() + 0x60), new int[] { 0 });

                    if (ZMBotBase != cwapi.FindDMAAddy(hProc, (IntPtr)(baseAddress.ToInt64() + PlayerBase.ToInt64() + 0x68), new int[] { 0 }))
                        ZMBotBase = cwapi.FindDMAAddy(hProc, (IntPtr)(baseAddress.ToInt64() + PlayerBase.ToInt64()) + 0x68, new int[] { 0 });

                    if (ZMBotBase != (IntPtr)0x0 && ZMBotBase != (IntPtr)0x68 && ZMBotListBase != cwapi.FindDMAAddy(hProc, ZMBotBase + ZM_Bot_List_Offset, new int[] { 0 }))
                        ZMBotListBase = cwapi.FindDMAAddy(hProc, ZMBotBase + ZM_Bot_List_Offset, new int[] { 0 });

                    // create new byte array for player coordinates, reads them, and then sets the XYZ coordinates accordingly
                    byte[] playerCoords = new byte[12];
                    cwapi.ReadProcessMemory(hProc, PlayerPedPtr + PP_Coords, playerCoords, 12, out _);
                    var origx = BitConverter.ToSingle(playerCoords, 0);
                    var origy = BitConverter.ToSingle(playerCoords, 4);
                    var origz = BitConverter.ToSingle(playerCoords, 8);
                    // updates the current playerposition with a Vector3 created from the xyz coordinates
                    updatedPlayerPos = new Vector3((float)Math.Round(origx, 4), (float)Math.Round(origy, 4), (float)Math.Round(origz, 4));

                    // unused, no idea what i was doing - something something setting player to the last known position if the freezeplayer checkbox is checked
                    if (freezePlayer)
                    {
                        if (frozenPlayerPos == Vector3.Zero) frozenPlayerPos = (lastKnownPlayerPos == Vector3.Zero) ? updatedPlayerPos : lastKnownPlayerPos;

                        UpdatePlayerPosition(frozenPlayerPos);
                    }


                    // something something if the sliders dont match the player's position, set the player to that position, else just update the sliders (although they'd of course not match the players position when they ove so idk what i was attempting)
                    // i guess this mainly focused on moving the player around when frozen
                    bool needPosUpdate = false;

                    if (numericUpDown3.Value != (decimal)lastKnownPlayerPos.X)
                    {
                        lastKnownPlayerPos.X = (float)numericUpDown3.Value;
                        needPosUpdate = true;
                    }
                    else
                    {
                        numericUpDown3.Value = (decimal)updatedPlayerPos.X;
                    }

                    if (numericUpDown5.Value != (decimal)lastKnownPlayerPos.Y)
                    {
                        lastKnownPlayerPos.Y = (float)numericUpDown5.Value;
                        needPosUpdate = true;
                    }
                    else
                    {
                        numericUpDown5.Value = (decimal)updatedPlayerPos.Y;
                    }

                    if (numericUpDown4.Value != (decimal)lastKnownPlayerPos.Z)
                    {
                        lastKnownPlayerPos.Z = (float)numericUpDown4.Value;
                        needPosUpdate = true;
                    }
                    else
                    {
                        numericUpDown4.Value = (decimal)updatedPlayerPos.Z;
                    }

                    if (needPosUpdate)
                    {
                        UpdatePlayerPosition(lastKnownPlayerPos);
                    }


                    // on first loop set player speed in GUI and tool to ingame speed
                    if (playerSpeed < 0)
                    {
                        byte[] plrSpd = new byte[4];
                        cwapi.ReadProcessMemory(hProc, PlayerCompPtr + PC_RunSpeed, plrSpd, 4, out _);
                        trackBar1.Value = Convert.ToInt32(BitConverter.ToSingle(plrSpd, 0));
                        numericUpDown1.Value = Convert.ToDecimal(BitConverter.ToSingle(plrSpd, 0));
                    }

                    // if ammo is frozen, set all weapon ammo to 20
                    if (ammoFrozen)
                    {
                        for (int i = 1; i < 6; i++)
                        {
                            cwapi.WriteProcessMemory(hProc, PlayerCompPtr + PC_Ammo + (i * 0x4), 20, 4, out _);
                        }
                    }

                    // if godmode is checked, enable godmode, else disable it
                    if (checkBox1.Checked)
                    {
                        cwapi.WriteProcessMemory(hProc, PlayerCompPtr + PC_GodMode, 0xA0, 1, out _);
                    }
                    else
                    {
                        cwapi.WriteProcessMemory(hProc, PlayerCompPtr + PC_GodMode, 0x20, 1, out _);
                    }

                    // post 1.1.2 - combined tp zombies to cursor and 1HP zombies into a single loop, no point in looping twice for the same thing

                    if (checkBox4.Checked || checkBox5.Checked)
                    {
                        byte[] enemyPosBuffer = new byte[12];

                        if (checkBox5.Checked)
                        {
                            // gets current player position
                            byte[] playerHeadingXY = new byte[4];
                            byte[] playerHeadingZ = new byte[4];
                            cwapi.ReadProcessMemory(hProc, PlayerPedPtr + PP_Heading_XY, playerHeadingXY, 4, out _);
                            cwapi.ReadProcessMemory(hProc, PlayerPedPtr + PP_Heading_Z, playerHeadingZ, 4, out _);

                            // some stack overflow magic to get the direction the player is facing and getting a position in front of the player
                            var pitch = -ConvertToRadians(BitConverter.ToSingle(playerHeadingZ, 0));
                            var yaw = ConvertToRadians(BitConverter.ToSingle(playerHeadingXY, 0));
                            var x = Convert.ToSingle(Math.Cos(yaw) * Math.Cos(pitch));
                            var y = Convert.ToSingle(Math.Sin(yaw) * Math.Cos(pitch));
                            var z = Convert.ToSingle(Math.Sin(pitch));

                            // im guessing just a straight up BitConverter.GetBytes could have worked for writing vector3s to memory instead of this kinda messy solution
                            var newEnemyPos = updatedPlayerPos + (new Vector3(x, y, z) * zmTeleportDistance);

                            Buffer.BlockCopy(BitConverter.GetBytes(newEnemyPos.X), 0, enemyPosBuffer, 0, 4);
                            Buffer.BlockCopy(BitConverter.GetBytes(newEnemyPos.Y), 0, enemyPosBuffer, 4, 4);
                            Buffer.BlockCopy(BitConverter.GetBytes(newEnemyPos.Z), 0, enemyPosBuffer, 8, 4);
                        }

                        for (int i = 0; i < 90; i++)
                        {
                            // if 1hp zombies is checked, set all zombie hp and max hp to 1
                            if (checkBox4.Checked)
                            {
                                cwapi.WriteProcessMemory(hProc, ZMBotListBase + (ZM_Bot_ArraySize_Offset * i) + ZM_Bot_Health, 1, 4, out _);
                                cwapi.WriteProcessMemory(hProc, ZMBotListBase + (ZM_Bot_ArraySize_Offset * i) + ZM_Bot_MaxHealth, 1, 4, out _);
                            }

                            // if tp zombies is checked, set their position to the position we got earlier
                            if (checkBox5.Checked)
                            {
                                cwapi.WriteProcessMemory(hProc, ZMBotListBase + (ZM_Bot_ArraySize_Offset * i) + ZM_Bot_Coords, enemyPosBuffer, 12, out _);
                            }
                        }
                    }

                    //if (checkBox4.Checked)
                    //{
                    //    for (int i = 0; i < 90; i++)
                    //    {
                    //        cwapi.WriteProcessMemory(hProc, ZMBotListBase + (ZM_Bot_ArraySize_Offset * i) + ZM_Bot_Health, 1, 4, out _);
                    //        cwapi.WriteProcessMemory(hProc, ZMBotListBase + (ZM_Bot_ArraySize_Offset * i) + ZM_Bot_MaxHealth, 1, 4, out _);
                    //    }
                    //}

                    //if (checkBox5.Checked)
                    //{
                    //    byte[] playerHeadingXY = new byte[4];
                    //    byte[] playerHeadingZ = new byte[4];


                    //    cwapi.ReadProcessMemory(hProc, PlayerPedPtr + PP_Heading_XY, playerHeadingXY, 4, out _);
                    //    cwapi.ReadProcessMemory(hProc, PlayerPedPtr + PP_Heading_Z, playerHeadingZ, 4, out _);

                    //    var pitch = -ConvertToRadians(BitConverter.ToSingle(playerHeadingZ, 0));
                    //    var yaw = ConvertToRadians(BitConverter.ToSingle(playerHeadingXY, 0));

                    //    var x = Convert.ToSingle(Math.Cos(yaw) * Math.Cos(pitch));
                    //    var y = Convert.ToSingle(Math.Sin(yaw) * Math.Cos(pitch));
                    //    var z = Convert.ToSingle(Math.Sin(pitch));

                    //    var newEnemyPos = updatedPlayerPos + (new Vector3(x, y, z) * zmTeleportDistance);

                    //    byte[] enemyPosBuffer = new byte[12];

                    //    Buffer.BlockCopy(BitConverter.GetBytes(newEnemyPos.X), 0, enemyPosBuffer, 0, 4);
                    //    Buffer.BlockCopy(BitConverter.GetBytes(newEnemyPos.Y), 0, enemyPosBuffer, 4, 4);
                    //    Buffer.BlockCopy(BitConverter.GetBytes(newEnemyPos.Z), 0, enemyPosBuffer, 8, 4);

                    //    for (int i = 0; i < 90; i++)
                    //    {
                    //        cwapi.WriteProcessMemory(hProc, ZMBotListBase + (ZM_Bot_ArraySize_Offset * i) + ZM_Bot_Coords, enemyPosBuffer, 12, out _);
                    //    }
                    //}

                    // infrared vision toggle
                    if (checkBox6.Checked)
                    {
                        cwapi.WriteProcessMemory(hProc, PlayerCompPtr + PC_InfraredVision, new byte[] { 0x10 }, 1, out _);
                    }
                    else
                    {
                        cwapi.WriteProcessMemory(hProc, PlayerCompPtr + PC_InfraredVision, new byte[] { 0x0 }, 1, out _);
                    }

                    if (checkBox7.Checked)
                    {
                        cwapi.WriteProcessMemory(hProc, PlayerCompPtr + PC_Points, 100000, 8, out _);
                    }

                    // xp modifiers
                    if (checkBox8.Checked)
                    {
                        // if the value is 0 or less, set both weapon xp and player xp modifiers to their defaults in the game (in case someone has a legit xp booster or something)
                        if (numericUpDown6.Value <= 0)
                        {
                            byte[] _tempBuffer = new byte[4];
                            cwapi.ReadProcessMemory(hProc, (IntPtr)(baseAddress.ToInt64() + ZMXPScaleBase.ToInt64()) + XPGun_Offset, _tempBuffer, 4, out _);
                            numericUpDown6.Value = (decimal)BitConverter.ToSingle(_tempBuffer, 0);
                            trackBar6.Value = (int)BitConverter.ToSingle(_tempBuffer, 0);
                        }

                        if (numericUpDown7.Value <= 0)
                        {
                            byte[] _tempBuffer = new byte[4];
                            cwapi.ReadProcessMemory(hProc, (IntPtr)(baseAddress.ToInt64() + ZMXPScaleBase.ToInt64()) + XPUNK02_Offset, _tempBuffer, 4, out _);
                            numericUpDown7.Value = (decimal)BitConverter.ToSingle(_tempBuffer, 0);
                            trackBar7.Value = (int)BitConverter.ToSingle(_tempBuffer, 0);
                        }

                        // writes the xp modifier values to memory, i guess just a straight up BitConverter.GetBytes could've worked without the creation of the byte buffers
                        byte[] tempBuffer1 = new byte[4];
                        Buffer.BlockCopy(BitConverter.GetBytes(gunXpModifier), 0, tempBuffer1, 0, 4);
                        byte[] tempBuffer2 = new byte[4];
                        Buffer.BlockCopy(BitConverter.GetBytes(xpModifier), 0, tempBuffer2, 0, 4);

                        cwapi.WriteProcessMemory(hProc, (IntPtr)(baseAddress.ToInt64() + ZMXPScaleBase.ToInt64()) + XPGun_Offset, tempBuffer1, 4, out _);
                        cwapi.WriteProcessMemory(hProc, (IntPtr)(baseAddress.ToInt64() + ZMXPScaleBase.ToInt64()) + XPUNK02_Offset, tempBuffer2, 4, out _);
                    }

                    // gets zombies left and updates the label of the Zombies left counter
                    byte[] zombiesLeft = new byte[4];
                    cwapi.ReadProcessMemory(hProc, ZMGlobalBase + ZM_Global_ZMLeftCount, zombiesLeft, 4, out _);
                    lblZombiesLeft.Text = "Zombies Left: " + BitConverter.ToInt32(zombiesLeft, 0);

                    // updates the lastknownplayerpos variable to the current players position
                    lastKnownPlayerPos = updatedPlayerPos;

                    // if there was an error with these memory reads/writes, output it to the gui console
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        ConsoleOut(Marshal.GetLastWin32Error().ToString());
                    }
                }
                // if an error happened during the loop, output that to the gui console
                catch (Exception err)
                {
                    ConsoleOut(err.Message);
                }
            }
        }

        // TP player button, for testing only
        private void button1_Click(object sender, EventArgs e)
        {

            frozenPlayerPos.X = (float)numericUpDown3.Value;
            frozenPlayerPos.Y = (float)numericUpDown5.Value;
            frozenPlayerPos.Z = (float)numericUpDown4.Value;
            UpdatePlayerPosition(frozenPlayerPos);
        }

        // syncs gun and player xp modifer trackbars and numuds
        private void trackBar6_Scroll(object sender, EventArgs e)
        {
            gunXpModifier = (float)trackBar6.Value;
            numericUpDown6.Value = (decimal)trackBar6.Value;
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            gunXpModifier = (float)numericUpDown6.Value;
            trackBar6.Value = (int)numericUpDown6.Value;
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            xpModifier = (float)trackBar7.Value;
            numericUpDown7.Value = (decimal)trackBar7.Value;
        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            xpModifier = (float)numericUpDown7.Value;
            trackBar7.Value = (int)numericUpDown7.Value;
        }

        // attempted to set the player position in a similar way to how I TP the zombies but it doesn't appear to work
        public void UpdatePlayerPosition(Vector3 pos)
        {
            byte[] tempPosBuffer = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(pos.X), 0, tempPosBuffer, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(pos.Y), 0, tempPosBuffer, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(pos.Z), 0, tempPosBuffer, 8, 4);

            cwapi.WriteProcessMemory(hProc, PlayerPedPtr + PP_Coords, tempPosBuffer, 12, out _);
        }

        // outputs a string to the gui console
        public void ConsoleOut(string str)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(ConsoleOut), new object[] { str });
                return;
            }
            DateTime curDT = DateTime.Now;
            txtConsole.AppendText(curDT.ToString("d-MMM-yyyy HH:mm:ss - ") + str + Environment.NewLine);
        }

        // updates a label with new text and colour, with multi-thread support for when the background worker needs to do it
        public void UpdateLabel(Label label, string text, string color = "Black")
        {
            if (this.InvokeRequired)
            {
                label.Invoke((MethodInvoker)delegate ()
                {
                    label.Text = text;
                    label.ForeColor = Color.FromName(color);
                });
                return;
            }
            label.Text = text;
            label.ForeColor = Color.FromName(color);
        }

        // something for something that i dont use anymore
        public string ToHex(object num)
        {
            return string.Format("0x{0:X}", num);
        }

        // converts a degree angle to radians, for the tp zombies feature
        public double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }
    }
}