#include "RemnantSaveGame.h"

URemnantSaveGame::URemnantSaveGame() {
    this->NewGame = true;
    this->HasMainCampaign = false;
    this->LastActiveRootSlot = -2;
    this->ZoneIDGen = 2;
    this->QuestIDGen = 1;
    this->TimePlayed = 0.00f;
}

float URemnantSaveGame::GetSavedTimePlayed() const {
    return 0.0f;
}

float URemnantSaveGame::GetCurrentTimePlayed(UObject* WorldContextObject) {
    return 0.0f;
}


