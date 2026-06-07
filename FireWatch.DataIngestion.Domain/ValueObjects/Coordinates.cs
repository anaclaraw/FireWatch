using FireWatch.DataIngestion.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace FireWatch.DataIngestion.Domain.ValueObjects;

public sealed record Coordinates
{
    public double Latitude { get; }
    public double Longitude { get; }

    public Coordinates(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new InvalidCoordinatesException(
                $"Latitude inválida: {latitude}. Deve estar entre -90 e 90.");

        if (longitude is < -180 or > 180)
            throw new InvalidCoordinatesException(
                $"Longitude inválida: {longitude}. Deve estar entre -180 e 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";
}
