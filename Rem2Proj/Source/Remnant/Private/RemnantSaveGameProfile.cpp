#include "RemnantSaveGameProfile.h"
#include "Templates/SubclassOf.h"

URemnantSaveGameProfile::URemnantSaveGameProfile() {
    this->ActiveCharacterIndex = -1;
    this->bHasEverCompletedIntro = false;
    this->bNeedsCheatedSaveConfirmation = false;
    this->SavedMatchmakingRegion = EMatchmakingRegions::UNKNOWN;
    this->SurvivalBossesKilled = 0;
    this->SurvivalCoopBossesKilled = 0;
    this->SurvivalItemsAcquired = 0;
}

bool URemnantSaveGameProfile::UpdateAccountCurrency(TSubclassOf<AItem> CurrencyType, int32 Quantity) {
    return false;
}

bool URemnantSaveGameProfile::HasAccountAward(TSubclassOf<UAccountAward> Award) {
    return false;
}

TArray<TSoftClassPtr<URemnantArchetype>> URemnantSaveGameProfile::GetUnlockedArchetypes(bool bIncludedEntitledArchetypes) {
    return TArray<TSoftClassPtr<URemnantArchetype>>();
}

int32 URemnantSaveGameProfile::GetAccountCurrencyForItemType(TSubclassOf<AItem> CurrencyType) {
    return 0;
}

bool URemnantSaveGameProfile::CheckHasEverCompletedIntro() {
    return false;
}


