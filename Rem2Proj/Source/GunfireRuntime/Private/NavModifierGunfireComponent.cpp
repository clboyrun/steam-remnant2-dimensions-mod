#include "NavModifierGunfireComponent.h"
#include "NavAreas/NavArea_Null.h"

UNavModifierGunfireComponent::UNavModifierGunfireComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bCanEverAffectNavigation = true;
    this->EnabledAreaClass = UNavArea_Null::StaticClass();
    this->DisabledAreaClass = UNavArea_Null::StaticClass();
    this->Shape = ENavModifierGunfireShape::Box;
    this->CylinderRadius = 50.00f;
    this->CylinderHeight = 100.00f;
    this->IsEnabled = true;
}

void UNavModifierGunfireComponent::SetState(bool Enabled) {
}


