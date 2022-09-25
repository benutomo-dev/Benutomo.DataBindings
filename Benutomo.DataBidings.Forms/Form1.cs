using System.Diagnostics;
using System.Windows.Forms;
using Benutomo.DataBindings;

namespace Benutomo.DataBidings.Forms
{
    public partial class Form1 : Form
    {
        [EnableNotificationSupport]
        [ChangedEvent]
        private int Counter1
        {
            get => _Counter1();
            set => _Counter1(value);
        }

        [EnableNotificationSupport]
        [ChangedEvent]
        private int Counter2
        {
            get => _Counter2();
            set => _Counter2(value);
        }

        public Form1()
        {
            InitializeComponent();

            bindingContext1.DefaultForwardSynchronizationHandler = action =>
            {
                Invoke(action);
            };

            bindingContext1.ForwardSyncError += ForwardSyncError;
            bindingContext1.BackwardSyncError += BackwardSyncError;


            bindingContext1.MakeBinding(textBox1, textBox1 => textBox1.Text, textBox2, textBox2 => textBox2.Text);
            bindingContext1.MakeBinding(textBox3, textBox3 => textBox3.Text, textBox2, textBox2 => textBox2.Text);
            bindingContext1.MakeBinding(this, @this => @this.Counter2, numericUpDown1, numericUpDown1 => numericUpDown1.Value);
            bindingContext1.MakeBinding(numericUpDown1, numericUpDown1 => numericUpDown1.Value, numericUpDown2, numericUpDown2 => numericUpDown2.Value);
            bindingContext1.MakeBinding(numericUpDown2, numericUpDown2 => numericUpDown2.Value, numericUpDown3, numericUpDown3 => numericUpDown3.Value);

            Task.Run(async () => {
                for (int i = 0; i < 100000; i++)
                {
                    await Task.Delay(10);
                    Counter2++;
                }
            });
        }

        private void ForwardSyncError(Exception ex)
        {
            Debug.WriteLine($"FowardSyncError: {ex.Message}");
        }
        private void BackwardSyncError(Exception ex)
        {
            Debug.WriteLine($"BackwardSyncError: {ex.Message}");
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Counter1++;
        }
    }
}