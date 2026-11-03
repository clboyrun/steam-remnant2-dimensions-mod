#include "Gunfire3DNavRenderingComponent.h"

UGunfire3DNavRenderingComponent::UGunfire3DNavRenderingComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bCanEverAffectNavigation = false;
    this->bIsEditorOnly = true;
    this->bSelectable = false;
}


