using Model;
using View;

namespace Controller.AI
{
    public class Controller_AI
    {
        Model_DEMO model;
        View_DEMO view;

        public Controller_AI(Model_DEMO model, View_DEMO view)
        {
            this.model = model;
            this.view = view;
            model.OnAwaitDrawCard += OnAwaitDrawCard;
        }

        private void OnAwaitDrawCard(int playerIndex, int drawIndex)
        {
            if(playerIndex == 0)
                return;

            model.TriggerDrawCard = true;
        }
    }
}