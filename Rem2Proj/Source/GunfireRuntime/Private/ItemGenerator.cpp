#include "ItemGenerator.h"

UItemGenerator::UItemGenerator() {
    this->Chance = 10;
    this->LevelMin = 0;
    this->LevelMax = 100;
}

void UItemGenerator::GenerateItems(FItemGenRequest Request, TArray<FItemGenResult>& Results) {
}


