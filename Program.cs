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
            Updater.RunSelfTest();
            MainForm.RunSelfTest();
            ApplicationConfiguration.Initialize();
            using var form = new MainForm();
            form.CreateControl();
            if (form.BackColor != Color.FromArgb(7, 9, 9))
                throw new InvalidOperationException("A identidade visual Ronaldinho não foi aplicada.");
            var pages = form.Controls.Find("WindowPage", true);
            if (pages.Length != 2)
                throw new InvalidOperationException("A interface deve conter exatamente duas janelas.");
            if (pages.Any(page =>
                    page.Controls.Find("ProtectionEnabled", true).Length != 1))
                throw new InvalidOperationException("Cada janela deve permitir ativar ou desativar a proteção.");
            if (pages.Any(page => page.Controls.Find("LifePercent", true).Length != 1
                                  || page.Controls.Find("SpotsList", true).Length != 1
                                  || page.Controls.Find("SessionGroup", true).Length != 1))
                throw new InvalidOperationException("A interface visual compacta não foi montada por completo.");
            return 0;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
