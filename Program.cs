namespace ControlarTela;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                NativeMethods.RunSelfTest();
                Recognition.RunSelfTest();
                Updater.RunSelfTest();
                MainForm.RunSelfTest();
                ApplicationConfiguration.Initialize();
                using var form = new MainForm(false)
                {
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(-32000, -32000),
                    ShowInTaskbar = false,
                    Size = new Size(1280, 720)
                };
                form.Show();
                Application.DoEvents();
                if (form.BackColor != Color.FromArgb(7, 9, 9))
                    throw new InvalidOperationException("A identidade visual Ronaldinho não foi aplicada.");
                var pages = form.Controls.Find("WindowPage", true);
                if (pages.Length != 2)
                    throw new InvalidOperationException("A interface deve conter exatamente duas janelas.");
                if (form.Controls.Find("ProtectionEnabled", true).Length != 2)
                    throw new InvalidOperationException("Cada janela deve permitir ativar ou desativar a proteção.");
                if (pages.Any(page => page.Controls.Find("LifePercent", true).Length != 1
                                      || page.Controls.Find("SpotsList", true).Length != 1
                                      || page.Controls.Find("SessionGroup", true).Length != 1
                                      || page.Controls.Find("AdvancedGroup", true).Length != 1
                                      || page.Controls.Find("LifeModule", true).Length != 1
                                      || page.Controls.Find("SpotsModule", true).Length != 1))
                    throw new InvalidOperationException("A interface visual compacta não foi montada por completo.");
                foreach (var name in new[] { "RefreshWindows", "StartStop", "WindowSelector", "ProtectionEnabled", "UseSpots" })
                    AssertInside(RequiredVisible(form, name));
                foreach (var text in new[] { "＋", "REMOVER" })
                    AssertInside(RequiredVisibleButton(form, text));
                var spotsList = (CheckedListBox)RequiredVisible(form, "SpotsList");
                if (spotsList.ClientSize.Height / spotsList.ItemHeight < 5)
                    throw new InvalidOperationException("A lista de spots deve exibir cinco linhas completas.");
                var advancedToggle = form.Controls.Find("AdvancedToggle", true).OfType<CheckBox>().FirstOrDefault()
                                     ?? throw new InvalidOperationException("Controle de opções avançadas não encontrado.");
                advancedToggle.Checked = true;
                Application.DoEvents();
                foreach (var text in new[] { "JANELA DE SPOTS", "ABRIR MENU", "BOTÃO TELEPORTAR", "⌖ RECALIBRAR", "▷ TESTAR" })
                    AssertInside(RequiredVisibleButton(form, text));
                var advancedGroup = RequiredVisible(form, "AdvancedGroup");
                var viewport = RequiredVisible(form, "Viewport");
                if (advancedGroup.Width < viewport.ClientSize.Width - 80)
                    throw new InvalidOperationException("As opções avançadas foram comprimidas horizontalmente.");
                return 0;
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "ControlarTela-self-test-error.txt"), exception.ToString());
                return 1;
            }
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }

    static Control RequiredVisible(Control root, string name) =>
        root.Controls.Find(name, true).FirstOrDefault(control => control.Visible)
        ?? throw new InvalidOperationException($"Controle visível não encontrado: {name}.");

    static Button RequiredVisibleButton(Control root, string text) =>
        FindAll(root).OfType<Button>().FirstOrDefault(button => button.Visible && button.Text == text)
        ?? throw new InvalidOperationException($"Botão visível não encontrado: {text}.");

    static IEnumerable<Control> FindAll(Control root)
    {
        foreach (Control control in root.Controls)
        {
            yield return control;
            foreach (var child in FindAll(control))
                yield return child;
        }
    }

    static void AssertInside(Control control)
    {
        const int borderTolerance = 2;
        var parent = control.Parent
                     ?? throw new InvalidOperationException($"Controle sem contêiner: {control.Name} {control.Text}.");
        if (control.Left < -borderTolerance || control.Top < -borderTolerance
            || control.Right > parent.ClientSize.Width + borderTolerance
            || control.Bottom > parent.ClientSize.Height + borderTolerance)
            throw new InvalidOperationException(
                $"Controle recortado: {control.Name} {control.Text}; bounds={control.Bounds}; parent={parent.ClientSize}.");
    }
}
