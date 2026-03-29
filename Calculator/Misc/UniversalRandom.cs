using System;
namespace Calculator;

public static class UniversalRandom {
    private static Random? instance;

    public static Random Get() {
        if(instance is null) {
            instance = new Random();
        }

        return instance;
    }
}

