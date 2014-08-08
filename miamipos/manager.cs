﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace miamiPOS
{
    public partial class manager : Form
    {
        DataTable tablaProductos = null;
        DataTable tablaVentas = null;
        public manager()
        {
            InitializeComponent();
            dataGridViewProductos.DataSource = tablaProductos;

            // Para la pestaña de anticipos
            //dateTimePicker.Format = DateTimePickerFormat.Custom;
            //dateTimePicker.CustomFormat = "MM - yyyy";
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void manager_Load(object sender, EventArgs e)
        {
            miamiDB.getCategorias(ref comboBoxCategorias);
            miamiDB.getCategorias(ref cbCategoria);

            checkBoxEditMode.Enabled = false; //Deshabilitar el checkmox de modo editar
        }

        private void comboBoxCategorias_SelectedIndexChanged(object sender, EventArgs e)
        {
            Int32 idCategoria = Convert.ToInt32((comboBoxCategorias.SelectedItem as ComboboxItem).Value);
            Psql.execQuery("select  plu, barcode, nombre, precio, pesable, id_categoria from producto where id_categoria=" + idCategoria, ref tablaProductos);
            dataGridViewProductos.DataSource = tablaProductos;

            dataGridViewProductos.Columns["id_categoria"].Visible = false;

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            try
            {
                int selectedrowindex = dataGridViewProductos.SelectedCells[0].RowIndex;
                DataGridViewRow selectedRow = dataGridViewProductos.Rows[selectedrowindex]; 

                var plu = Convert.ToInt32( selectedRow.Cells["plu"].Value ); //plu de item seleccionado
                Psql.execScalar("delete from producto where plu=" + plu);
                dataGridViewProductos.Rows.RemoveAt( selectedrowindex );
            }
            catch
            {
                Console.WriteLine("Error al eliminar");
            }
        }

        //Esta funcion rellena las textbox de modo editar
        private void fillEditor(int plu, string barcode, string name, int price,int id_categoria, bool pesable)
        {
            tbPLU.Text = plu.ToString();
            tbBarcode.Text = barcode.ToString();
            tbName.Text = name.ToString();
            tbPrice.Text = price.ToString();
            checkBoxPesable.Checked = pesable;

            // Siguiente foreach para dejar la categoria del item seleccionada en modo editar
            foreach (ComboboxItem cbItem in this.cbCategoria.Items)
            {
                if (cbItem.Value == id_categoria)
                {
                    this.cbCategoria.SelectedItem = cbItem;
                }
            }
        }
        //Esta funcion retorna un objeto Producto con los valores obtenidos desde las textbox
        private Producto getFromEditor()
        {
            try
            {
                int plu;
                if (tbPLU.Text.Length < 1) plu = 0;
                else plu = Convert.ToInt32(tbPLU.Text);

                Int32 idCategoria = Convert.ToInt32((cbCategoria.SelectedItem as ComboboxItem).Value);
                return new Producto(plu, tbBarcode.Text, tbName.Text, Convert.ToInt32(tbPrice.Text), idCategoria, checkBoxPesable.Checked);
            }
            catch
            {
                throw new Exception("Producto en Editor no respeta la estructura deseada");
            }
        }
        private void buttonEdit_Click(object sender, EventArgs e)
        {
            try
            {
                int selectedrowindex = dataGridViewProductos.SelectedCells[0].RowIndex;
                DataGridViewRow selectedRow = dataGridViewProductos.Rows[selectedrowindex];

                var plu = selectedRow.Cells["plu"].Value; //plu de item seleccionado
                var barcode = selectedRow.Cells["barcode"].Value;
                var nombre = selectedRow.Cells["nombre"].Value;
                var precio = selectedRow.Cells["precio"].Value;
                var pesable = Convert.ToBoolean (selectedRow.Cells["pesable"].Value );
                var id_categoria = selectedRow.Cells["id_categoria"].Value;


                fillEditor((int)plu, barcode.ToString() , (string)nombre, Convert.ToInt32(precio), (int)id_categoria, (bool)pesable);

                checkBoxEditMode.Checked = true; //activar modo editar
              
            }
            catch(Exception E)
            {
                Console.WriteLine("Nada que Editar \r catch: " + E.Message);
            }
        }

        private void buttonNewItem_Click(object sender, EventArgs e)
        {
            foreach (Control x in this.groupBox1.Controls)
            {
                if (x is TextBox)
                {
                    ((TextBox)x).Clear();
                }
            }
            checkBoxEditMode.Checked = false;
            cbCategoria.SelectedItem = null;
            tbPLU.Focus();
        }
        private void newItem()
        {
            foreach (Control x in this.groupBox1.Controls)
            {
                if (x is TextBox)
                {
                    ((TextBox)x).Clear();
                }
            }
            checkBoxEditMode.Checked = false;
            cbCategoria.SelectedItem = null;
            tbPLU.Focus();
        }

        private void buttonSaveItem_Click(object sender, EventArgs e)
        {
            try
            {
                Producto productoAEditar = getFromEditor();
                Console.WriteLine(productoAEditar.toSQL(checkBoxEditMode.Checked));

                Psql.execInsert(productoAEditar.toSQL(checkBoxEditMode.Checked));

                // Buscar producto editado o creado
                textBoxSearch.Text = tbPLU.Text;
                button1_Click(buttonSearch, EventArgs.Empty);

                newItem();

                MessageBox.Show("EXITO");
            }
            catch (Exception E)
            {
                MessageBox.Show("ERROR AL ACTUALIZAR :\r" + E.Message);
            }

        }

        private void checkBoxEditMode_CheckedChanged(object sender, EventArgs e)
        {
            tbPLU.ReadOnly = checkBoxEditMode.Checked; //si esta en modo editar el textbox del plu se bloquea
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int id;
            string query = "select  plu, barcode, nombre, precio, pesable, id_categoria from producto ";
            string whereCond = "";
            //Si la busqueda esta vacia
            if (textBoxSearch.Text.Length < 1)          whereCond ="where plu=(select last_value from producto_plu_seq)";
            // Si la busqueda no esta vacia
            else
            {
                try
                { //Si el campo es numerico
                    id = Convert.ToInt32(textBoxSearch.Text);
                    // Si es un plu
                    if (textBoxSearch.Text.Length <= 5)    whereCond ="where plu="+ id;
                    //si es un codigo de barras
                    else                        whereCond =String.Format("where barcode='{0}'" , id);
                }
                catch(Exception E)
                { // Si el campo no es numerico
                    whereCond = String.Format("where upper(nombre) LIKE upper('%{0}%')", textBoxSearch.Text);
                }
            }

            Psql.execQuery(query + whereCond, ref tablaProductos);
            dataGridViewProductos.DataSource = tablaProductos;
            dataGridViewProductos.Columns["id_categoria"].Visible = false;
        }

        private void coneccionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Abre la pagina web de la interfaz guardada en las settings serverRemote
            System.Diagnostics.Process.Start(miamiPOS.Properties.Settings.Default.serverRemote);
        }


        /*
         * Bloque para la pestaña pagos que incluye colaciones anticipos y facturas
         * 
         * 
         */

        private void dateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            // dateTimePicker.Value.Month
            int doy = dateTimePicker.Value.DayOfYear;
            int year = dateTimePicker.Value.Year;
            var query = String.Format("select nombre,sum(cantidad) as Cantidad,sum(venta_producto.total) as Dinero from producto,venta_producto,venta where venta.id_venta = venta_producto.id_venta and venta_producto.plu=producto.plu and producto.pesable={0} and extract(year from fecha)={1} and extract(doy from fecha)={2} group by nombre order by Dinero DESC"
                ,checkBoxPesableventa.Checked.ToString(),year,doy);
            Psql.execQuery(query, ref tablaVentas);
            dataGridViewVentas.DataSource = tablaVentas;

            dataGridViewVentas.Columns["dinero"].DefaultCellStyle.Format = "$##,###,###";
            //dataGridViewProductos.Columns["id_categoria"].Visible = false;
        }

        private void checkBoxPesableventa_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePicker_ValueChanged(sender, e);
        }

        private void textBoxSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            {
                if (e.KeyChar == (char)13)
                {
                    button1_Click(sender, new EventArgs());
                }
            }
        }
    }
}
