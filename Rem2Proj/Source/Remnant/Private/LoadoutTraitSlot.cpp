#include "LoadoutTraitSlot.h"

ULoadoutTraitSlot::ULoadoutTraitSlot() {
    this->SlotIndex = 0;
    this->Type = NULL;
}

UTraitsComponent* ULoadoutTraitSlot::GetTraitsComponent(ACharacterGunfire* Character) const {
    return NULL;
}


