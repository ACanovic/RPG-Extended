using Engine;
using System;
using System.Windows.Forms;

namespace RPG
{
    public partial class TradingScreen : Form
    {
        private Player _currentPlayer;

        public TradingScreen(Player player)
        {
            _currentPlayer = player;

            InitializeComponent();

            //Numeric column display style
            DataGridViewCellStyle rightAlignedCellStyle = new DataGridViewCellStyle();
            rightAlignedCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            //Populate player inventory datagrid
            dgvMyItems.RowHeadersVisible = false;
            dgvMyItems.AutoGenerateColumns = false;

            //Hidden column that holds item ID, to know which item to sell
            dgvMyItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ItemID",
                Visible = false
            });

            dgvMyItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Width = 100,
                DataPropertyName = "Description"
            });

            dgvMyItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Qty",
                Width = 30,
                DefaultCellStyle = rightAlignedCellStyle,
                DataPropertyName = "Quantity"
            });

            dgvMyItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Price",
                Width = 35,
                DefaultCellStyle = rightAlignedCellStyle,
                DataPropertyName = "Price"
            });

            dgvMyItems.Columns.Add(new DataGridViewButtonColumn
            {
                Text = "Sell 1",
                UseColumnTextForButtonValue = true,
                Width = 50,
                DataPropertyName = "ItemID"
            });

            //Bind player inventory to the DGV
            dgvMyItems.DataSource = _currentPlayer.Inventory;

            //Call this when user clicks a row
            dgvMyItems.CellClick += dgvMyItems_CellClick;

            //Populate vendor inventory datagrid
            dgvVendorItems.RowHeadersVisible = false;
            dgvVendorItems.AutoGenerateColumns = false;

            //Hidden column that holds item ID, to know which item to sell
            dgvVendorItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ItemID",
                Visible = false
            });

            dgvVendorItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Width = 100,
                DataPropertyName = "Description"
            });

            dgvVendorItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Price",
                Width = 35,
                DefaultCellStyle = rightAlignedCellStyle,
                DataPropertyName = "Price"
            });

            dgvVendorItems.Columns.Add(new DataGridViewButtonColumn
            {
                Text = "Buy 1",
                UseColumnTextForButtonValue = true,
                Width = 50,
                DataPropertyName = "ItemID"
            });

            //Bind vendor inventory to the DGV
            dgvVendorItems.DataSource = _currentPlayer.CurrentLocation.VendorWorkingHere.Inventory;

            //Call this when user clicks a row
            dgvVendorItems.CellClick += dgvVendorItems_CellClick;
        }

        private void dgvMyItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //5th column has Sell 1 button
            if(e.ColumnIndex == 4)
            {
                //Get ID value of item from hidden 1st column
                var itemID = dgvMyItems.Rows[e.RowIndex].Cells[0].Value;

                //Get Item object for selected row
                Item itemBeingSold = World.ItemByID(Convert.ToInt32(itemID));

                if(itemBeingSold.Price == World.UNSELLABLE_ITEM_PRICE)
                {
                    MessageBox.Show("You cannot sell the " + itemBeingSold.Name);
                }
                else
                {
                    //Remove a copy of the item from the player's inventory
                    _currentPlayer.RemoveItemFromInventory(itemBeingSold);

                    _currentPlayer.Gold += itemBeingSold.Price;
                }
            }
        }

        private void dgvVendorItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //4th column has Buy 1 button
            if (e.ColumnIndex == 3)
            {
                //Get ID value of item from hidden 1st column
                var itemID = dgvVendorItems.Rows[e.RowIndex].Cells[0].Value;

                //Get Item object for selected row
                Item itemBeingBought = World.ItemByID(Convert.ToInt32(itemID));

                if (itemBeingBought.Price <= _currentPlayer.Gold)
                {
                    _currentPlayer.AddItemToInventory(itemBeingBought);

                    _currentPlayer.Gold -= itemBeingBought.Price;
                }
                else
                {
                    MessageBox.Show("You do not have enough gold to buy the " + itemBeingBought.Name);
                }
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
