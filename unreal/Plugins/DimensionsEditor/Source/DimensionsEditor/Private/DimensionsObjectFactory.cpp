#include "DimensionsObjectFactory.h"

UDimensionsObjectFactory::UDimensionsObjectFactory()
{
    bCreateNew = true;
    bEditAfterNew = false;
    bEditorImport = false;
    SupportedClass = UObject::StaticClass();
}

UClass* UDimensionsObjectFactory::ResolveSupportedClass()
{
    return ObjectClass ? ObjectClass.Get() : UObject::StaticClass();
}

UObject* UDimensionsObjectFactory::FactoryCreateNew(UClass* InClass, UObject* InParent, FName InName,
    EObjectFlags Flags, UObject* Context, FFeedbackContext* Warn)
{
    UClass* Class = ObjectClass ? ObjectClass.Get() : InClass;
    if (!Class || Class->HasAnyClassFlags(CLASS_Abstract | CLASS_Deprecated))
    {
        return nullptr;
    }
    return NewObject<UObject>(InParent, Class, InName, Flags | RF_Public | RF_Standalone | RF_Transactional);
}
