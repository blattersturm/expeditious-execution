using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace BaseRush.Client
{
    public class CelebrationWinner : BaseScript
    {
        public static bool Enabled { get; set; }

        private Scaleform[] m_scaleforms;

        private int m_startTime;
        private int m_endTime;

        private int m_valueHandle;

        public static string TeamName { get; set; } = "IAA";

        public static string WinnerType { get; set; } = "CELEB_WINNER";

        public static int IAAScore { get; set; } = 1337;

        public static int FIBScore { get; set; } = 69;

        public static Action OnComplete { get; set; }

        [Command("win")]
        public void Win()
        {
            Enabled = true;
        }

        [Tick]
        public async Task OnTick()
        {
            if (!Enabled)
            {
                if (m_scaleforms != null)
                {
                    foreach (var sf in m_scaleforms)
                    {
                        sf.Dispose();
                    }

                    m_scaleforms = null;
                }

                return;
            }

            if (m_scaleforms == null)
            {
                m_scaleforms = new [] { new Scaleform("mp_celebration"), new Scaleform("mp_celebration_bg"), new Scaleform("mp_celebration_fg") };

                while (!m_scaleforms[0].IsLoaded || !m_scaleforms[1].IsLoaded || !m_scaleforms[2].IsLoaded)
                {
                    await Delay(0);
                }

                foreach (var gfx in m_scaleforms)
                {
                    gfx.CallFunction("CLEANUP", "mywall");
                    gfx.CallFunction("CREATE_STAT_WALL", "mywall", "HUD_COLOUR_MICHAEL", 100.0f);
                    gfx.CallFunction("ADD_WINNER_TO_WALL", "mywall", WinnerType, GetPlayerName(PlayerId()), false, false, true, TeamName);
                    gfx.CallFunction("ADD_STAT_NUMERIC_TO_WALL", "mywall", "IAA", IAAScore.ToString(), false, true);
                    gfx.CallFunction("ADD_STAT_NUMERIC_TO_WALL", "mywall", "FIB", FIBScore.ToString(), false, true);
                    gfx.CallFunction("ADD_BACKGROUND_TO_WALL", "mywall", 75.0f, 0);
                    gfx.CallFunction("SHOW_STAT_WALL", "mywall");
                }

                BeginScaleformMovieMethod(m_scaleforms[0].Handle, "GET_TOTAL_WALL_DURATION");
                m_valueHandle = EndScaleformMovieMethodReturn();

                m_startTime = GetGameTimer();
                m_endTime = GetGameTimer() + 9000;
            }

            if (m_valueHandle != 0)
            {
                // IS_SCALEFORM_MOVIE_METHOD_RETURN_VALUE_READY
                if (GetScaleformMovieFunctionReturnBool(m_valueHandle))
                {
                    // get_scaleform_movie_method_return_value_int
                    var time = GetScaleformMovieFunctionReturnInt(m_valueHandle);
                    m_endTime = m_startTime + time;

                    m_valueHandle = 0;
                }
            }

            m_scaleforms[1].Render2D();
            m_scaleforms[0].Render2D();
            m_scaleforms[2].Render2D();

            if (GetGameTimer() > m_endTime)
            {
                Enabled = false;

                if (OnComplete != null)
                {
                    OnComplete();
                    OnComplete = null;
                }
            }
        }
    }
}