using System;
using System.Drawing;
using System.Windows.Forms;

namespace Turbo_Flapper
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Создаем форму
            var view = new GameForm();

            // Получаем размеры труб после инициализации формы
            var topPipeSize = view.PipeTop.Size;
            var bottomPipeSize = view.PipeBottom.Size;

            // Создаем презентер
            var presenter = new GamePresenter(view, topPipeSize, bottomPipeSize);

            // Настраиваем обработчик таймера
            view.GameTimer.Tick += (s, e) => presenter.Update();

            // Инициализируем
            presenter.Initialize();

            Application.Run(view);
        }
    }
}
