#include "LoadoutEquipmentSlot.h"

ULoadoutEquipmentSlot::ULoadoutEquipmentSlot() {
    this->SlotType = NULL;
    this->bRecordEquipmentMods = true;
}

AEquipment* ULoadoutEquipmentSlot::GetEquipment(ACharacterGunfire* Character) const {
    return NULL;
}


