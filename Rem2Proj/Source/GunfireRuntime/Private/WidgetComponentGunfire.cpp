#include "WidgetComponentGunfire.h"

UWidgetComponentGunfire::UWidgetComponentGunfire(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->DrawScale = 1.00f;
    this->bUseDrawWidgetOverrides = true;
    this->bManualDestroyOnExit = false;
}


