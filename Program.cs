namespace ControlarTela;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            NativeMethods.RunSelfTest();
            Recognition.RunSelfTest();
            MainForm.RunSelfTest();
            ApplicationConfiguration.Initialize();
            using var form = new MainForm();
            form.CreateControl();
            if (form.BackColor != Color.FromArgb(7, 9, 9))
                throw new InvalidOperationException("A identidade visual Ronaldinho não foi aplicada.");
            var tabs = form.Controls.OfType<TabControl>().Single();
            if (tabs.TabCount != 2)
                throw new InvalidOperationException("A interface deve conter exatamente duas janelas.");
            if (tabs.TabPages.Cast<TabPage>().Any(page =>
                    page.Controls.Find("ProtectionEnabled", true).Length != 1))
                throw new InvalidOperationException("Cada janela deve permitir ativar ou desativar a proteção.");
            return 0;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
