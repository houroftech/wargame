﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Wargame.Core;

namespace Wargame
{
    public partial class BattleForm : Form
    {
        private GameData Game { get; set; }
        private StringBuilder Messages { get; set; }
        private GameEngine Engine { get; set; }

        public BattleForm()
        {
            InitializeComponent();
            btnAttack.Enabled = false;
            btnCreateGame.Text = "Pick Players";
            Messages = new StringBuilder();
            Game = (new GameFactory()).CreateNewGame();
            Engine = new GameEngine(Game);
            InitializeVendor();
            InitializeRoster();
        }

        private void InitializeRoster()
        {
            DataGridViewCell cell = new DataGridViewTextBoxCell();

            var grids = new List<DataGridView>() { dataGridViewAvailableCharacter, dataGridViewMyTeam, dataGridViewOpponentTeam };
            foreach (var i in grids)
            {
                i.AutoGenerateColumns = false;
                i.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    CellTemplate = cell,
                    Name = "Name",
                    HeaderText = "Name",
                    DataPropertyName = "Name",
                });
                i.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    CellTemplate = cell,
                    Name = "HP",
                    HeaderText = "HP",
                    DataPropertyName = "MaxHP",
                });
                i.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    CellTemplate = cell,
                    Name = "Strength",
                    HeaderText = "STR",
                    DataPropertyName = "DieName",
                });
                i.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    CellTemplate = cell,
                    Name = "Class",
                    HeaderText = "Class",
                    DataPropertyName = "Class",
                });
            }

            RefreshPlayerGold();

            dataGridViewAvailableCharacter.DataSource = Game.AvailableCharacters;
            dataGridViewMyTeam.DataSource = Game.Team1;
            dataGridViewOpponentTeam.DataSource = Game.Team2;
            
            //also populated view on equipment manager
            dataGridViewEMTeamRoster.DataSource = Game.Team1;
        }

        private void BtnCreateGame_Click(object sender, EventArgs e)
        {
            if (!Game.TeamsFull)
            {
                tabControlMain.SelectTab(tabControlMain.TabPages["tabRosterMgmt"]);
                return;
            }
            Engine.StartRound(firstRound: true);
            Messages.AppendLine($"Next up:\r\n  {Game.RoundOrder.Peek().PrintStats()}");
            RefreshLog();
            btnAttack.Enabled = true;
        }

        private void InitializeVendor()
        {
            DataGridViewCell cell = new DataGridViewTextBoxCell();

            var grids = new List<DataGridView>() {dataGridViewVendor, dataGridViewPlayerInventory, dataGridViewEMCharInventory };
            foreach (var i in grids)
            {
                i.AutoGenerateColumns = false;
                i.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    CellTemplate = cell,
                    Name = "Name",
                    HeaderText = "Item",
                    DataPropertyName = "Description",
                });
                i.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    CellTemplate = cell,
                    Name = "Price",
                    HeaderText = "Price",
                    DataPropertyName = "Price",
                });
            }

            dataGridViewVendor.DataSource = Game.Vendor;
            dataGridViewPlayerInventory.DataSource = Game.PlayerInventory;

            dataGridViewEMPlayerInventory.DataSource = Game.PlayerInventory;
        }
        private void RefreshLog()
        {
            txtLog.Text = Messages.ToString();
            txtRoundLog.Text = Game.InitiativeList();
            txtTeam1.Text = Game.TeamRoster(1);
            txtTeam2.Text = Game.TeamRoster(2);
            roundLabel.Text = $"Round {Game.RoundNumber}";
            Messages.Clear();
        }
        
        private void RefreshPlayerGold()
        {
            txtPlayerGold.Text = $"Player Gold: {Game.PlayerGold}";
        }

        private void BtnAttack_Click(object sender, EventArgs e)
        {
            btnAttack.Enabled = false;

            var status = Engine.ProcessAttack();
            Messages.AppendLine($"{status}\r\n");

            if (!Game.RoundOrder.Any()) Engine.StartRound();

            btnAttack.Enabled = !Game.GameOver;
            if (!Game.GameOver) Messages.AppendLine($"Next up:\r\n  {Game.RoundOrder.Peek().PrintStats()}");
            RefreshLog();
        }

        private void BtnPurchase_Click(object sender, EventArgs e)
        {
            if (!Game.Vendor.Any()) return;
            var selectedItem = (Item)dataGridViewVendor.CurrentRow.DataBoundItem;
            if (Game.PlayerGold >= selectedItem.Price)
            {
                Game.PlayerGold -= selectedItem.Price;
                Game.Vendor.Remove(selectedItem);
                Game.PlayerInventory.Add(selectedItem);
                RefreshPlayerGold();
            }
            else
            {
                MessageBox.Show("Not enough Gold!");
            }
        }

        private void BtnDraft_Click(object sender, EventArgs e)
        {
            if (StartGameIfTeamFull()) return;
            var selectedCharacter = (Character)dataGridViewAvailableCharacter.CurrentRow.DataBoundItem;
            Game.AvailableCharacters.Remove(selectedCharacter);
            Game.Team1.Add(selectedCharacter);      

            var oppCharacter = Game.AvailableCharacters.OrderBy(x => Guid.NewGuid()).First();
            Game.AvailableCharacters.Remove(oppCharacter);
            Game.Team2.Add(oppCharacter);
            StartGameIfTeamFull();
        }

        private bool StartGameIfTeamFull()
        {
            if (Game.TeamsFull)
            {
                MessageBox.Show("All set. Teams full. Starting Game!");
                tabControlMain.SelectTab(1);
                btnCreateGame.Text = "Start Game";
                btnCreateGame.PerformClick();
            }
            return !Game.AvailableCharacters.Any() || Game.TeamsFull;
        }

        private void BtnDraftTeamIntroScreen_Click(object sender, EventArgs e)
        {
            tabControlMain.SelectTab(tabControlMain.TabPages["tabRosterMgmt"]);
        }

        private void BtnEquipItem(object sender, EventArgs e)
        {
            var selectedCharacter = (Character)dataGridViewEMTeamRoster.CurrentRow?.DataBoundItem;
            var selectedItem = (Item)dataGridViewEMPlayerInventory.CurrentRow?.DataBoundItem;

            dataGridViewEMCharInventory.DataSource = selectedCharacter.Inventory.Inventory;
            Game.PlayerInventory.Remove(selectedItem);
            selectedCharacter.Inventory.Inventory.Add(selectedItem);
          
        }

        private void dataGridViewEMTeamRoster_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null)
                return;
            if (dgv.CurrentRow.Selected)
            {
                var selectedCharacter = (Character)dataGridViewEMTeamRoster.CurrentRow?.DataBoundItem;
                dataGridViewEMCharInventory.DataSource = selectedCharacter.Inventory.Inventory;
            }
        }

        private void dataGridViewEMTeamRoster_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null)
                return;
            if (dgv.CurrentRow.Selected)
            {
                var selectedCharacter = (Character)dataGridViewEMTeamRoster.CurrentRow?.DataBoundItem;
                dataGridViewEMCharInventory.DataSource = selectedCharacter.Inventory.Inventory;
                lblCharInv.Text = $"{selectedCharacter.Name}'s Inventory"; 
                lblCharRoster.Text = $"{selectedCharacter.Name} Currently Selected";
            }
        }
    }
}
